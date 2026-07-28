using System.Text.Json;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Repositories;

/// <summary>
/// Pure EF Core implementation of <see cref="ICartOrderRepository"/>.
///
/// Methods mirror the original stored procedures 1-to-1:
///   InsertCartOrderAsync        ← usp_cart_insert_cart_order (header + all items, in one transaction)
///   SelectCartOrderAsync        ← usp_cart_select_cart_order + usp_cart_select_cart_order_item
///   FindExistingVendorOrderCodeByKeyAsync ← cart_order_message lookup (quote-key pivot, G14)
///
/// SP behaviours preserved:
///   G3  — vendor_order_code via EXEC usp_next_id @Type=3 (ids table)
///   G10 — cart_order_item_json_log audit row after items inserted
///   G11 — line_item offset for add-to-cart (MAX existing line_item + 1)
///   G12 — CBCART → WRCART reroute hack (SP section 2.2.1)
///   G13 — CD (S/H) line date sync via ExecuteSqlAsync
/// </summary>
public sealed class CartOrderRepository : ICartOrderRepository
{
    // partner_configuration_id = 15 → currency override for partner orders (SP 1.3.2)
    private const byte PartnerCurrencyConfigId = 15;
    // Default currency_id (1 = USD) when no match found (SP 1.3.3)
    private const byte DefaultCurrencyId = 1;

    private readonly AppDbContext _db;
    private readonly ILogger<CartOrderRepository> _logger;

    public CartOrderRepository(AppDbContext db, ILogger<CartOrderRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PUBLIC — ICartOrderRepository
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    public async Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // ── Header + related rows (cart_order_partner, route, message, json) ──
            var (vendorOrderCode, order) = await InsertCartOrderHeaderAsync(request, ct);

            // ── G11: max line_item for add-to-cart scenario ────────────────────
            // For a brand-new cart this is 0.  For a re-submitted cart we offset
            // new items so line_item values remain unique.
            int maxLineItem = 0;
            if (!string.IsNullOrWhiteSpace(request.VendorOrderCode))
            {
                maxLineItem = await _db.CartOrderItems
                    .Where(i => i.CartOrderId == order.CartOrderId)
                    .Select(i => (int?)i.LineItem)
                    .MaxAsync(ct) ?? 0;
            }

            // ── Items ──────────────────────────────────────────────────────────
            for (var idx = 0; idx < request.Items.Count; idx++)
                await InsertCartOrderItemAsync(order.CartOrderId, request.Items[idx], maxLineItem + idx + 1, ct);

            // Reload items into the tracked entity so we can compute totals
            await _db.Entry(order).Collection(o => o.Items).LoadAsync(ct);

            // ── G10: JSON audit log (one row per cart-create call) ─────────────
            _db.CartOrderItemJsonLogs.Add(new CartOrderItemJsonLog
            {
                CartOrderId = order.CartOrderId,
                ItemJson    = JsonSerializer.Serialize(request.Items),
                BundleJson  = JsonSerializer.Serialize(
                    request.Items.Select(x => new
                    {
                        keycode = x.Keycode,
                        license_attribute_license_value = x.LicenseAttributeLicenseValue
                    })),
                InsertDate = DateTime.UtcNow
            });

            // ── SP 5.5: total_amount / sub_total_amount ────────────────────────
            var totalAmount = order.Items.Sum(i => (decimal)i.UnitPrice * i.Quantity);
            order.TotalAmount    = totalAmount;
            order.SubTotalAmount = totalAmount;

            await _db.SaveChangesAsync(ct);

            // ── G12: CBCART → WRCART reroute (SP section 2.2.1) ───────────────
            if (string.Equals(order.SiteId, "CBCART", StringComparison.OrdinalIgnoreCase)
                && order.CurrencyId.HasValue
                && new byte[] { 1, 2, 3, 4, 29 }.Contains(order.CurrencyId.Value)
                && request.Items.Any(i => i.Years == 0))
            {
                order.Locale    = "en_US";
                order.SiteId    = "WRCART";
                order.OrderType = "WRCART";
                await _db.SaveChangesAsync(ct);
            }

            // ── G13: CD (S/H) line date sync ───────────────────────────────────
            // S/H items (product_family_id = 8) inherit start/expiration from the
            // non-CD product in the same cart_item_bundle_id.
            await _db.Database.ExecuteSqlAsync(
                $"""
                UPDATE coi2
                SET   coi2.start_date      = coi.start_date,
                      coi2.expiration_date = coi.expiration_date
                FROM  dbo.cart_order_item coi
                INNER JOIN dbo.product p  ON p.product_id  = coi.product_id
                INNER JOIN dbo.cart_order_item coi2
                    ON  coi2.cart_order_id       = coi.cart_order_id
                    AND coi2.cart_item_bundle_id = coi.cart_item_bundle_id
                INNER JOIN dbo.product p2 ON p2.product_id = coi2.product_id
                WHERE coi.cart_order_id = {order.CartOrderId}
                  AND (p.product_family_id <> 8 OR p.product_family_id IS NULL)
                  AND p2.product_family_id = 8
                """, ct);

            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "CartOrderRepository: inserted cart_order_id={CartOrderId} vendor_order_code={VendorOrderCode} total={TotalAmount}",
                order.CartOrderId, vendorOrderCode, totalAmount);

            return vendorOrderCode;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default)
    {
        var header = await SelectCartOrderHeaderAsync(vendorOrderCode, ct);
        if (header is null) return null;

        // Message key — same for all items in the order
        var messageKey = await _db.CartOrderMessages
            .Where(m => m.CartOrderId == header.CartOrderId)
            .Select(m => (Guid?)m.MessageKey)
            .FirstOrDefaultAsync(ct);
        var messageKeyStr  = messageKey?.ToString();
        var currencySymbol = GetCurrencySymbol(header.CurrencyCode);

        var items = await SelectCartOrderItemsAsync(
            header.CartOrderId, messageKeyStr, currencySymbol, ct);

        // Group by cart_item_bundle_id — legacy contract: "items": { "1": [{...}] }
        var itemsDict = items
            .GroupBy(i => (i.CartItemBundleId ?? 0).ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        return new CartOrderResponse
        {
            CartOrderId       = header.CartOrderId,
            VendorOrderCode   = header.VendorOrderCode,
            SiteId            = header.SiteId,
            OfferAmount       = header.OfferAmount,
            TotalAmount       = header.TotalAmount,
            SubTotalAmount    = header.SubTotalAmount,
            TaxAmount         = header.TaxAmount,
            SalesOrderDate    = header.SalesOrderDate,
            Locale            = header.Locale,
            InsertDate        = header.InsertDate,
            InsertBy          = header.InsertBy,
            ModifiedDate      = header.ModifiedDate,
            ModifiedBy        = header.ModifiedBy,
            CartOrderStatusId = header.CartOrderStatusId,
            CurrencyId        = header.CurrencyId,
            CurrencyCode      = header.CurrencyCode,
            UserIp            = header.UserIp,
            PartnerKey        = header.PartnerKey,
            CartJson          = header.CartJson,
            Route             = header.Route,
            Items             = itemsDict,
            IsExternal        = false,
            UsePaymentech     = true,
            Customers         = null,
            Cybersource       = null,
            SafeAccountEmail  = null,
            SubTotalAmountFmt = FormatCurrency(header.SubTotalAmount, currencySymbol),
            TaxAmountFmt      = FormatCurrency(header.TaxAmount, currencySymbol),
            TotalAmountFmt    = FormatCurrency(header.TotalAmount, currencySymbol),
            OfferAmountFmt    = FormatCurrency(header.OfferAmount, currencySymbol)
        };
    }

    /// <inheritdoc/>
    public async Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key, CancellationToken ct = default)
    {
        if (!Guid.TryParse(key, out var keyGuid)) return null;

        return await (
            from m in _db.CartOrderMessages
            join o in _db.CartOrders on m.CartOrderId equals o.CartOrderId
            where m.MessageKey == keyGuid
            select o.VendorOrderCode
        ).FirstOrDefaultAsync(ct);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIVATE — usp_cart_insert_cart_order (header rows)
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task<(string VendorOrderCode, CartOrder Order)> InsertCartOrderHeaderAsync(
        CartOrderCreateRequest request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // SP 1.1 — resolve partner_id from partner_key (UUID)
        int? partnerId = null;
        if (!string.IsNullOrWhiteSpace(request.PartnerKey)
            && Guid.TryParse(request.PartnerKey, out var partnerGuid))
        {
            partnerId = await _db.Partners
                .Where(p => p.PartnerKey == partnerGuid)
                .Select(p => (int?)p.PartnerId)
                .SingleOrDefaultAsync(ct);
        }

        // SP 1.3 — resolve currency_id (request → partner config → default USD)
        byte? currencyId = null;
        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            currencyId = await _db.Currencies
                .Where(c => c.CurrencyCode == request.CurrencyCode)
                .Select(c => (byte?)c.CurrencyId)
                .SingleOrDefaultAsync(ct);
        }
        if (currencyId is null && partnerId is not null)
        {
            var partnerCurrencyCode = await _db.PartnerConfigurationPartners
                .Where(cp => cp.PartnerId == partnerId
                          && cp.PartnerConfigurationId == PartnerCurrencyConfigId)
                .Select(cp => cp.ConfigurationValue)
                .SingleOrDefaultAsync(ct);

            if (!string.IsNullOrWhiteSpace(partnerCurrencyCode))
                currencyId = await _db.Currencies
                    .Where(c => c.CurrencyCode == partnerCurrencyCode)
                    .Select(c => (byte?)c.CurrencyId)
                    .SingleOrDefaultAsync(ct);
        }
        currencyId ??= DefaultCurrencyId;

        // SP 2.1 — generate vendor_order_code if not supplied
        var vendorOrderCode = request.VendorOrderCode;
        if (string.IsNullOrWhiteSpace(vendorOrderCode))
        {
            var prefix = await _db.CartSiteIdOrderCodePrefixes
                .Where(x => x.SiteId == request.SiteId)
                .Select(x => x.VendorOrderCodePrefix)
                .SingleOrDefaultAsync(ct)
                ?? request.SiteId[..Math.Min(3, request.SiteId.Length)].ToUpper();

            var nextId = await GetNextVendorOrderIdAsync(ct);
            vendorOrderCode = $"{prefix}{nextId:D8}";
        }

        // SP 2.2 — INSERT cart_order
        var order = new CartOrder
        {
            VendorOrderCode = vendorOrderCode,
            OrderType       = request.SiteId,
            SiteId          = request.SiteId,
            SiteUrl         = request.SiteId,
            SalesOrderDate  = request.SalesOrderDate?.Date ?? now.Date,
            SubmissionDate  = now,
            Locale          = request.Locale,
            UserIp          = request.UserIp,
            CurrencyId      = currencyId.Value,
            InsertDate      = now,
            InsertBy        = "api",
            ModifiedDate    = now,
            ModifiedBy      = "api"
        };
        _db.CartOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        // SP 2.3 — INSERT cart_order_partner (with partner_account_id via EF LINQ)
        if (partnerId is not null)
        {
            int? partnerAccountId = null;
            if (!string.IsNullOrWhiteSpace(request.AccountUserName))
            {
                partnerAccountId = await (
                    from pa in _db.PartnerAccounts
                    join a in _db.Accounts on pa.AccountId equals a.AccountId
                    where pa.PartnerId == partnerId
                       && a.AccountUserName == request.AccountUserName
                    select (int?)pa.PartnerAccountId
                ).SingleOrDefaultAsync(ct);
            }
            _db.CartOrderPartners.Add(new CartOrderPartner
            {
                CartOrderId      = order.CartOrderId,
                PartnerId        = partnerId.Value,
                PartnerAccountId = partnerAccountId
            });
        }

        // SP 2.4 — INSERT cart_order_route
        if (!string.IsNullOrWhiteSpace(request.RoutingAction))
        {
            _db.CartOrderRoutes.Add(new CartOrderRoute
            {
                CartOrderId   = order.CartOrderId,
                RoutingAction = request.RoutingAction,
                InsertDate    = now
            });
        }

        // SP 2.5 — INSERT cart_order_message (when message_key is a valid GUID)
        if (!string.IsNullOrWhiteSpace(request.Key) && Guid.TryParse(request.Key, out var msgKeyGuid))
        {
            var licenseId = await _db.LicenseKeys
                .Where(k => k.LicenseKeyValue == msgKeyGuid)
                .Select(k => (int?)k.LicenseId)
                .SingleOrDefaultAsync(ct);

            _db.CartOrderMessages.Add(new CartOrderMessage
            {
                CartOrderId             = order.CartOrderId,
                MessageKey              = msgKeyGuid,
                MessageCampaignId       = request.MessageCampaignId,
                MessageCampaignPlatform = request.MessageCampaignPlatform,
                CartDiscountId          = request.CartDiscountId,
                LicenseId               = licenseId
            });
        }

        // SP 2.6 — INSERT cart_json
        var cartJson = BuildCartExtensionJson(request);
        if (cartJson is not null)
        {
            _db.CartJsons.Add(new CartJson
            {
                CartOrderId = order.CartOrderId,
                Json        = cartJson
            });
        }

        await _db.SaveChangesAsync(ct);
        return (vendorOrderCode, order);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIVATE — usp_cart_insert_cart_order_item (one call per item)
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task InsertCartOrderItemAsync(
        int cartOrderId, CartOrderItemRequest item, int lineItem, CancellationToken ct)
    {
        // Resolve retail price from product when no override supplied
        decimal? retailPrice = null;
        if (item.UnitPrice is null)
        {
            retailPrice = await _db.Products
                .Where(p => p.ProductId == item.ProductId)
                .Select(p => p.RetailPrice)
                .FirstOrDefaultAsync(ct);
        }

        var unitPrice = item.UnitPrice ?? retailPrice ?? 0m;
        var listPrice = retailPrice ?? unitPrice;
        var now       = DateTime.UtcNow;

        var cartItem = new CartOrderItem
        {
            CartOrderId                  = cartOrderId,
            LineItem                     = lineItem,
            ProductId                    = item.ProductId,
            // SP 5.3.2: license_seats used as quantity for business products
            Quantity                     = item.Quantity ?? item.LicenseSeats ?? 1,
            StorageGb                    = item.StorageGb,
            ListPrice                    = listPrice,
            UnitPrice                    = unitPrice,
            UsagePrice                   = item.UsagePrice,
            StartDate                    = item.StartDate,
            ExpirationDate               = item.ExpirationDate,
            CartItemBundleId             = item.CartItemBundleId,
            ItemHierarchyId              = (byte?)item.ItemHierarchyId,
            LicenseAttributeLicenseValue = item.LicenseAttributeLicenseValue,
            VendorOrderItemCode          = item.VendorOrderItemCode,
            Discount                     = item.Discount,
            CartDiscountMethodId         = item.CartDiscountMethodId,
            CartDiscountId               = item.CartDiscountId,
            OpportunityLineItemId        = item.OpportunityLineItemId,
            ProductLocale                = item.Locale,
            InsertDate                   = now,
            ModifiedDate                 = now
        };
        _db.CartOrderItems.Add(cartItem);
        await _db.SaveChangesAsync(ct);

        // INSERT cart_order_item_json (vault / platform / retention / pricing-level dimensions)
        var itemJson = BuildCartOrderItemJson(item);
        if (itemJson is not null)
        {
            _db.CartOrderItemJsons.Add(new CartOrderItemJson
            {
                CartOrderItemId        = cartItem.CartOrderItemId,
                CartOrderItemJsonValue = itemJson,
                InsertDate             = now,
                ModifiedDate           = now
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIVATE — usp_cart_select_cart_order (header)
    // ═══════════════════════════════════════════════════════════════════════════

    private sealed record OrderHeaderData(
        int CartOrderId, string VendorOrderCode, string SiteId,
        decimal? OfferAmount, decimal? TotalAmount, decimal SubTotalAmount,
        decimal? TaxAmount, DateTime? SalesOrderDate, string Locale,
        DateTime InsertDate, string? InsertBy, DateTime? ModifiedDate, string? ModifiedBy,
        byte CartOrderStatusId, int CurrencyId, string CurrencyCode,
        string? UserIp, string? PartnerKey, string? CartJson,
        CartOrderRouteInfo? Route);

    private async Task<OrderHeaderData?> SelectCartOrderHeaderAsync(
        string vendorOrderCode, CancellationToken ct)
    {
        var row = await (
            from co in _db.CartOrders
            join cu in _db.Currencies on co.CurrencyId equals (byte)cu.CurrencyId
            join cp in _db.CartOrderPartners on co.CartOrderId equals cp.CartOrderId into cpJoin
            from cp in cpJoin.DefaultIfEmpty()
            join p in _db.Partners on cp.PartnerId equals p.PartnerId into pJoin
            from p in pJoin.DefaultIfEmpty()
            join j in _db.CartJsons on co.CartOrderId equals j.CartOrderId into jJoin
            from j in jJoin.DefaultIfEmpty()
            where co.VendorOrderCode == vendorOrderCode
            select new
            {
                co.CartOrderId,
                co.VendorOrderCode,
                co.SiteId,
                co.OfferAmount,
                co.TotalAmount,
                co.SubTotalAmount,
                co.TaxAmount,
                co.SalesOrderDate,
                co.Locale,
                co.InsertDate,
                co.InsertBy,
                co.ModifiedDate,
                co.ModifiedBy,
                co.CartOrderStatusId,
                CurrencyId   = (int)cu.CurrencyId,
                cu.CurrencyCode,
                co.UserIp,
                PartnerKey   = p != null ? p.PartnerKey.ToString() : null,
                CartJson     = j != null ? j.Json : null
            }
        ).SingleOrDefaultAsync(ct);

        if (row is null) return null;

        // Build route URL from cart_order_route + cart_order_message
        var routingAction = await _db.CartOrderRoutes
            .Where(r => r.CartOrderId == row.CartOrderId)
            .Select(r => (string?)r.RoutingAction)
            .FirstOrDefaultAsync(ct);

        var msgKey = await _db.CartOrderMessages
            .Where(m => m.CartOrderId == row.CartOrderId)
            .Select(m => (Guid?)m.MessageKey)
            .FirstOrDefaultAsync(ct);

        var route = BuildRouteInfo(row.Locale, routingAction, msgKey?.ToString());

        return new OrderHeaderData(
            row.CartOrderId, row.VendorOrderCode!, row.SiteId,
            row.OfferAmount, row.TotalAmount, row.SubTotalAmount,
            row.TaxAmount, row.SalesOrderDate, row.Locale,
            row.InsertDate, row.InsertBy, row.ModifiedDate, row.ModifiedBy,
            row.CartOrderStatusId, row.CurrencyId, row.CurrencyCode,
            row.UserIp, row.PartnerKey, row.CartJson, route);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIVATE — usp_cart_select_cart_order_item
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task<List<CartOrderItemResponse>> SelectCartOrderItemsAsync(
        int cartOrderId, string? messageKey, string currencySymbol, CancellationToken ct)
    {
        // Build dependent-item map: bundleId → primaryItemId (hierarchy 1)
        var primaryByBundle = await _db.CartOrderItems
            .Where(i => i.CartOrderId == cartOrderId && i.ItemHierarchyId == 1)
            .Select(i => new { i.CartItemBundleId, i.CartOrderItemId })
            .ToListAsync(ct);
        var dependentMap = primaryByBundle
            .Where(x => x.CartItemBundleId.HasValue)
            .ToDictionary(x => x.CartItemBundleId!.Value, x => x.CartOrderItemId);

        var rows = await (
            from i  in _db.CartOrderItems
            join p  in _db.Products                 on i.ProductId                     equals p.ProductId
            join pf in _db.ProductFamilies           on p.ProductFamilyId               equals (int?)pf.ProductFamilyId into pfJoin
            from pf in pfJoin.DefaultIfEmpty()
            join plp in _db.ProductLineProducts      on p.ProductId                     equals plp.ProductId  into plpJoin
            from plp in plpJoin.DefaultIfEmpty()
            join prl in _db.ProductLines             on plp.ProductLineId               equals prl.ProductLineId into prlJoin
            from prl in prlJoin.DefaultIfEmpty()
            join t  in _db.ProductTypes              on p.ProductTypeId                 equals t.ProductTypeId
            join ij in _db.CartOrderItemJsons        on i.CartOrderItemId               equals ij.CartOrderItemId into ijJoin
            from ij in ijJoin.DefaultIfEmpty()
            join plc in _db.ProductLicenseCategories on p.ProductId                     equals plc.ProductId into plcJoin
            from plc in plcJoin.DefaultIfEmpty()
            join lc in _db.LicenseCategories         on plc.LicenseCategoryId           equals lc.LicenseCategoryId into lcJoin
            from lc in lcJoin.DefaultIfEmpty()
            join kt in _db.LicenseKeycodeTypes       on p.LicenseKeycodeTypeId          equals kt.LicenseKeycodeTypeId into ktJoin
            from kt in ktJoin.DefaultIfEmpty()
            join y  in _db.ProductYears              on p.ProductId                     equals y.ProductId into yJoin
            from y  in yJoin.DefaultIfEmpty()
            join s  in _db.ProductSeats              on p.ProductId                     equals s.ProductId into sJoin
            from s  in sJoin.DefaultIfEmpty()
            join v  in _db.LicenseAttributeLicenseValues
                on i.LicenseAttributeLicenseValue    equals v.LicenseAttributeLicenseValueId into vJoin
            from v  in vJoin.DefaultIfEmpty()
            join il in _db.CartOrderItemLicenses     on i.CartOrderItemId               equals il.CartOrderItemId into ilJoin
            from il in ilJoin.DefaultIfEmpty()
            where i.CartOrderId == cartOrderId
            select new
            {
                i.CartOrderItemId, i.CartOrderId, i.LineItem,
                i.Quantity, i.StorageGb, i.OrderItemOfferAmount,
                i.ListPrice, i.UnitPrice, i.UnitPricePreVat,
                i.TaxItemTotal, i.UsagePrice, i.ProductId,
                i.StartDate, i.ExpirationDate,
                i.CartItemBundleId, i.ItemHierarchyId,
                i.LicenseAttributeLicenseValue,
                i.VendorOrderItemCode, i.OrderItemUpdateTypeId,
                i.Discount, i.CartDiscountMethodId, i.CartDiscountId,
                i.OpportunityLineItemId,
                p.ProductDescription, p.LicenseKeycodeTypeId,
                t.ProductTypeId, t.ProductTypeDescription,
                ProductFamilyDescription          = pf != null ? pf.ProductFamilyDescription : null,
                ProductLineCartType               = prl != null ? prl.ProductLineCartType : null,
                LicenseKeycodeTypeDescription     = kt != null ? kt.LicenseKeycodeTypeDescription : null,
                LicenseCategoryId                 = lc != null ? (int?)lc.LicenseCategoryId : null,
                LicenseCategoryName               = lc != null ? lc.LicenseCategoryName : null,
                LicenseCategoryDescription        = lc != null ? lc.LicenseCategoryDescription : null,
                MinOrderQuantity                  = lc != null ? lc.MinOrderQuantity : null,
                MaxOrderQuantity                  = lc != null ? lc.MaxOrderQuantity : null,
                Years                             = y != null ? (decimal?)y.Years : null,
                Seats                             = s != null ? (int?)s.Seats : null,
                LicenseAttributeLicenseValueDescr = v != null ? v.LicenseAttributeLicenseValueDescription : null,
                Keycode                           = il != null ? il.Keycode : null,
                ItemJsonRaw                       = ij != null ? ij.CartOrderItemJsonValue : null
            }
        ).ToListAsync(ct);

        return rows.Select(r =>
        {
            var jp = ParseItemJson(r.ItemJsonRaw);

            // equivalent_year_price: retail_price × years
            // NULL for usage-pricing-model-2 items under 1 TB with no item_total (SP logic)
            decimal? equivYearPrice = null;
            if (r.UnitPrice != 0 && r.Years.HasValue)
            {
                var isModel2Under1Tb = jp?.UsagePricingModelId == 2
                    && jp?.ItemTotal is null
                    && (r.StorageGb ?? 0) < 1024;
                if (!isModel2Under1Tb)
                    equivYearPrice = (decimal)r.UnitPrice * r.Years.Value;
            }

            // DependentCartOrderItemId: for hierarchy-2 items, the hierarchy-1 partner
            int? dependentId = r.ItemHierarchyId == 2 && r.CartItemBundleId.HasValue
                ? dependentMap.GetValueOrDefault(r.CartItemBundleId.Value)
                : null;

            // Computed sub-totals
            decimal? subTotalList  = r.ListPrice  != 0 ? (decimal?)r.ListPrice  * r.Quantity : null;
            decimal? subTotal      = r.UnitPrice  != 0 ? (decimal?)r.UnitPrice  * r.Quantity : null;
            decimal? subTotalPreVat= r.UnitPricePreVat.HasValue
                ? r.UnitPricePreVat * r.Quantity : null;
            decimal? subTotalEquiv = equivYearPrice.HasValue
                ? equivYearPrice * r.Quantity : null;

            return new CartOrderItemResponse
            {
                CartOrderItemId                          = r.CartOrderItemId,
                CartOrderId                              = r.CartOrderId,
                LineItem                                 = r.LineItem,
                Quantity                                 = r.Quantity,
                Seats                                    = r.Seats,
                LicenseSeats                             = r.Seats,    // legacy alias
                StorageGb                                = r.StorageGb,
                Years                                    = r.Years,
                OrderItemOfferAmount                     = r.OrderItemOfferAmount,
                EquivalentYearPrice                      = equivYearPrice,
                ListPrice                                = r.ListPrice,
                UnitPrice                                = r.UnitPrice,
                UnitPricePreVat                          = r.UnitPricePreVat,
                TaxItemTotal                             = r.TaxItemTotal,
                UsagePrice                               = r.UsagePrice,
                Discount                                 = r.Discount,
                CartDiscountMethodId                     = r.CartDiscountMethodId,
                CartDiscountId                           = r.CartDiscountId,
                ProductId                                = r.ProductId,
                ProductDescription                       = r.ProductDescription,
                ProductTypeId                            = r.ProductTypeId,
                ProductTypeDescription                   = r.ProductTypeDescription,
                LicenseKeycodeTypeId                     = r.LicenseKeycodeTypeId,
                LicenseKeycodeTypeDescription            = r.LicenseKeycodeTypeDescription,
                LicenseCategoryId                        = r.LicenseCategoryId,
                LicenseCategoryName                      = r.LicenseCategoryName,
                LicenseCategoryDescription               = r.LicenseCategoryDescription,
                ProductFamilyDescription                 = r.ProductFamilyDescription,
                ProductLineCartType                      = r.ProductLineCartType,
                MinOrderQuantity                         = r.MinOrderQuantity,
                MaxOrderQuantity                         = r.MaxOrderQuantity,
                StartDate                                = r.StartDate,
                ExpirationDate                           = r.ExpirationDate,
                CartItemBundleId                         = r.CartItemBundleId,
                ItemHierarchyId                          = r.ItemHierarchyId,
                DependentCartOrderItemId                 = dependentId,
                Keycode                                  = r.Keycode,
                LicenseAttributeLicenseValue             = r.LicenseAttributeLicenseValue,
                LicenseAttributeLicenseValueDescription  = r.LicenseAttributeLicenseValueDescr,
                VendorOrderItemCode                      = r.VendorOrderItemCode,
                OrderItemUpdateTypeId                    = r.OrderItemUpdateTypeId,
                OpportunityLineItemId                    = r.OpportunityLineItemId,
                CartOrderItemJson                        = r.ItemJsonRaw,
                // Legacy fields
                MessageKey                               = messageKey,
                SubTotalListAmount                       = subTotalList,
                SubTotalAmount                           = subTotal,
                SubTotalAmountPreVat                     = subTotalPreVat,
                SubTotalEquivalentYearPrice              = subTotalEquiv,
                EstimatedMonthlyPrice                    = null,
                // Formatted amounts
                EquivalentYearPriceFmt                   = FormatCurrency(equivYearPrice, currencySymbol),
                ListPriceFmt                             = FormatCurrency(r.ListPrice, currencySymbol),
                UnitPriceFmt                             = FormatCurrency(r.UnitPrice, currencySymbol),
                UnitPricePreVatFmt                       = FormatCurrency(r.UnitPricePreVat, currencySymbol),
                UsagePriceFmt                            = FormatCurrency(r.UsagePrice, currencySymbol),
                SubTotalEquivalentYearPriceFmt           = FormatCurrency(subTotalEquiv, currencySymbol),
                SubTotalListAmountFmt                    = FormatCurrency(subTotalList, currencySymbol),
                SubTotalAmountFmt                        = FormatCurrency(subTotal, currencySymbol),
                SubTotalAmountPreVatFmt                  = FormatCurrency(subTotalPreVat, currencySymbol),
                // JSON-derived dimensions
                UsagePricingModelId                      = jp?.UsagePricingModelId,
                UsagePricingModelName                    = jp?.UsagePricingModelName,
                RetentionModelId                         = jp?.RetentionModelId,
                RetentionModelName                       = jp?.RetentionModelName,
                RetentionTerm                            = jp?.RetentionTerm,
                RetentionModelTypeId                     = jp?.RetentionModelTypeId,
                ProductPlatformId                        = jp?.ProductPlatformId,
                ProductPlatformName                      = jp?.ProductPlatformName,
                VaultId                                  = jp?.VaultId,
                VaultDatacenterName                      = jp?.VaultDatacenterName,
                Vault                                    = jp?.Vault,
                ProductPricingLevelId                    = jp?.ProductPricingLevelId,
                PricingLevelDescription                  = jp?.PricingLevelDescription
            };
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRIVATE — helpers
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calls usp_next_id @Type=3 via EF Core FromSqlRaw.
    /// The SP atomically increments ids.next_id WHERE id_type=3 and returns the new value.
    /// FromSqlRaw (without LINQ composition) executes the SQL directly — no subquery wrapping.
    /// </summary>
    private async Task<int> GetNextVendorOrderIdAsync(CancellationToken ct)
    {
        var rows = await _db.Set<NextIdResult>()
            .FromSqlRaw("EXEC usp_next_id @Type=3")
            .AsNoTracking()
            .ToListAsync(ct);

        return rows.FirstOrDefault()?.NextId
            ?? throw new InvalidOperationException(
                "usp_next_id @Type=3 returned no rows — verify ids table has a row for id_type=3");
    }

    private static CartOrderRouteInfo? BuildRouteInfo(string? locale, string? routingAction, string? key)
    {
        if (routingAction is null && key is null) return null;

        // "en_US" → "us/en"   |   "ja_JP" → "jp/ja"
        var localePath = "us/en";
        if (!string.IsNullOrWhiteSpace(locale) && locale.Contains('_'))
        {
            var p = locale.Split('_');
            if (p.Length == 2)
                localePath = $"{p[1].ToLower()}/{p[0].ToLower()}";
        }

        var qs = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(routingAction))
            qs.Append($"routing_action={Uri.EscapeDataString(routingAction)}");
        if (!string.IsNullOrWhiteSpace(key))
        {
            if (qs.Length > 0) qs.Append('&');
            qs.Append($"key={Uri.EscapeDataString(key)}");
        }

        return new CartOrderRouteInfo
        {
            Route = $"https://www.webroot.com/{localePath}/cart"
                  + (qs.Length > 0 ? $"?{qs}" : string.Empty)
        };
    }

    private static string GetCurrencySymbol(string? currencyCode) => currencyCode switch
    {
        "EUR" => "€",
        "GBP" => "£",
        "CAD" => "C$",
        "AUD" => "A$",
        _     => "$"
    };

    private static string? FormatCurrency(decimal? value, string symbol)
        => value.HasValue
            ? $"{symbol}{value.Value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)}"
            : null;

    private static string? FormatCurrency(decimal value, string symbol)
        => value == 0 ? null
            : $"{symbol}{value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)}";

    private static string? BuildCartExtensionJson(CartOrderCreateRequest r)
    {
        // Only store if at least one optional extension field was provided (SP 2.6)
        if (r.CurrencyCode is null && r.VendorOrderCode is null && r.PartnerKey is null
            && r.AccountUserName is null && r.RoutingAction is null && r.Key is null
            && r.MessageCampaignId is null && r.MessageCampaignPlatform is null
            && r.CartDiscountId is null && r.SalesOrderDate is null)
            return null;

        return JsonSerializer.Serialize(new
        {
            currency_code             = r.CurrencyCode,
            vendor_order_code         = r.VendorOrderCode,
            partner_key               = r.PartnerKey,
            account_user_name         = r.AccountUserName,
            routing_action            = r.RoutingAction,
            sales_order_date          = r.SalesOrderDate,
            message_campaign_id       = r.MessageCampaignId,
            message_campaign_platform = r.MessageCampaignPlatform,
            key                       = r.Key,
            cart_discount_id          = r.CartDiscountId
        });
    }

    private static string? BuildCartOrderItemJson(CartOrderItemRequest item)
    {
        if (item.UsagePricingModelId is null && item.RetentionModelId is null
            && item.ProductPlatformId is null && item.VaultId is null
            && item.ProductPricingLevelId is null && item.ItemTotal is null)
            return null;

        return JsonSerializer.Serialize(new
        {
            usage_pricing_model_id   = item.UsagePricingModelId,
            retention_model_id       = item.RetentionModelId,
            product_platform_id      = item.ProductPlatformId,
            vault_id                 = item.VaultId,
            product_pricing_level_id = item.ProductPricingLevelId,
            item_total               = item.ItemTotal
        });
    }

    private static ItemJsonDimensions? ParseItemJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<ItemJsonDimensions>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch { return null; }
    }

    // ── Internal DTO for cart_order_item_json parsing ─────────────────────────

    private sealed class ItemJsonDimensions
    {
        public int?     UsagePricingModelId  { get; init; }
        public string?  UsagePricingModelName { get; init; }
        public int?     RetentionModelId     { get; init; }
        public string?  RetentionModelName   { get; init; }
        public int?     RetentionTerm        { get; init; }
        public int?     RetentionModelTypeId { get; init; }
        public int?     ProductPlatformId    { get; init; }
        public string?  ProductPlatformName  { get; init; }
        public int?     VaultId              { get; init; }
        public string?  VaultDatacenterName  { get; init; }
        public string?  Vault                { get; init; }
        public int?     ProductPricingLevelId { get; init; }
        public string?  PricingLevelDescription { get; init; }
        public decimal? ItemTotal            { get; init; }
    }
}
