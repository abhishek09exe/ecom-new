using System.Text.Json;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ecom_new_api.Repositories;

/// <summary>
/// EF Core implementation of ICartOrderRepository.
/// Each private/public method maps 1-to-1 with a stored procedure:
///   InsertCartOrderHeaderAsync  ← usp_cart_insert_cart_order
///   InsertCartOrderItemAsync    ← usp_cart_insert_cart_order_item
///   SelectCartOrderHeaderAsync  ← usp_cart_select_cart_order
///   SelectCartOrderItemsAsync   ← usp_cart_select_cart_order_item
/// </summary>
public sealed class CartOrderRepository : ICartOrderRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<CartOrderRepository> _logger;

    // partner_configuration_id = 15 → currency override for partner orders (matches SP)
    private const int PartnerCurrencyConfigId = 15;
    // default currency_id when no match found (matches SP default)
    private const byte DefaultCurrencyId = 1;

    public CartOrderRepository(AppDbContext db, ILogger<CartOrderRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Composite: InsertCartOrderAsync ──────────────────────────────────────────
    // Orchestrates InsertCartOrderHeaderAsync + InsertCartOrderItemAsync per item.

    public async Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var vendorOrderCode = await InsertCartOrderHeaderAsync(request, ct);

            var cartOrderId = await _db.CartOrder
                .Where(o => o.VendorOrderCode == vendorOrderCode)
                .OrderByDescending(o => o.CartOrderId)  // pick the row we just inserted
                .Select(o => o.CartOrderId)
                .FirstAsync(ct);

            for (var i = 0; i < request.Items.Count; i++)
            {
                await InsertCartOrderItemAsync(cartOrderId, vendorOrderCode, request.Items[i], i + 1, ct);
            }

            await tx.CommitAsync(ct);
            return vendorOrderCode;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ── Composite: SelectCartOrderAsync ──────────────────────────────────────────
    // Combines SelectCartOrderHeaderAsync + SelectCartOrderItemsAsync.

    public async Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default)
    {
        var header = await SelectCartOrderHeaderAsync(vendorOrderCode, ct);
        if (header is null) return null;

        var messageKey = await GetOrderMessageKeyAsync(header.CartOrderId, ct);
        var currencySymbol = GetCurrencySymbol(header.CurrencyCode);
        var items = await SelectCartOrderItemsAsync(vendorOrderCode, ct);

        // Enrich items with computed sub-totals and formatted strings
        foreach (var item in items)
        {
            item.MessageKey = messageKey?.ToString();
            item.SubTotalListAmount = item.ListPrice.HasValue ? item.ListPrice * item.Quantity : null;
            item.SubTotalAmount = item.UnitPrice.HasValue ? item.UnitPrice * item.Quantity : null;
            item.SubTotalAmountPreVat = item.UnitPricePreVat.HasValue ? item.UnitPricePreVat * item.Quantity : null;
            item.SubTotalEquivalentYearPrice = item.EquivalentYearPrice.HasValue ? item.EquivalentYearPrice * item.Quantity : null;
            item.EstimatedMonthlyPrice = null;
            item.EquivalentYearPriceFmt = FormatCurrency(item.EquivalentYearPrice, currencySymbol);
            item.ListPriceFmt = FormatCurrency(item.ListPrice, currencySymbol);
            item.UnitPriceFmt = FormatCurrency(item.UnitPrice, currencySymbol);
            item.UnitPricePreVatFmt = FormatCurrency(item.UnitPricePreVat, currencySymbol);
            item.UsagePriceFmt = FormatCurrency(item.UsagePrice, currencySymbol);
            item.SubTotalEquivalentYearPriceFmt = FormatCurrency(item.SubTotalEquivalentYearPrice, currencySymbol);
            item.SubTotalListAmountFmt = FormatCurrency(item.SubTotalListAmount, currencySymbol);
            item.SubTotalAmountFmt = FormatCurrency(item.SubTotalAmount, currencySymbol);
            item.SubTotalAmountPreVatFmt = FormatCurrency(item.SubTotalAmountPreVat, currencySymbol);
        }

        // Group items by cart_item_bundle_id (key = bundle id as string)
        var itemsDict = items
            .GroupBy(i => (i.CartItemBundleId ?? 0).ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        return new CartOrderResponse
        {
            CartOrderId = header.CartOrderId,
            VendorOrderCode = header.VendorOrderCode,
            SiteId = header.SiteId,
            OfferAmount = header.OfferAmount,
            TotalAmount = header.TotalAmount,
            SubTotalAmount = header.SubTotalAmount,
            TaxAmount = header.TaxAmount,
            SalesOrderDate = header.SalesOrderDate,
            Locale = header.Locale,
            InsertDate = header.InsertDate,
            InsertBy = header.InsertBy,
            ModifiedDate = header.ModifiedDate,
            ModifiedBy = header.ModifiedBy,
            CartOrderStatusId = header.CartOrderStatusId,
            CurrencyId = (int)header.CurrencyId,
            CurrencyCode = header.CurrencyCode,
            UserIp = header.UserIp,
            PartnerKey = header.PartnerKey,
            CartJson = header.CartJson,
            Items = itemsDict,
            IsExternal = false,
            UsePaymentech = true,
            Customers = null,
            Cybersource = null,
            SafeAccountEmail = null,
            SubTotalAmountFmt = FormatCurrency(header.SubTotalAmount, currencySymbol),
            TaxAmountFmt = FormatCurrency(header.TaxAmount, currencySymbol),
            TotalAmountFmt = FormatCurrency(header.TotalAmount, currencySymbol),
            OfferAmountFmt = FormatCurrency(header.OfferAmount, currencySymbol)
        };
    }

    // ── usp_cart_insert_cart_order ────────────────────────────────────────────────

    public async Task<string> InsertCartOrderHeaderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var salesOrderDate = request.SalesOrderDate?.Date ?? now.Date;

        // 1.1) Resolve partner_id from partner_key
        int? partnerId = null;
        if (!string.IsNullOrWhiteSpace(request.PartnerKey) && Guid.TryParse(request.PartnerKey, out var partnerGuid))
        {
            partnerId = await _db.Partner
                .Where(p => p.PartnerKey == partnerGuid)
                .Select(p => (int?)p.PartnerId)
                .SingleOrDefaultAsync(ct);
        }

        // 1.2) Resolve currency_id (SP sections 1.3.1 → 1.3.2 → 1.3.3)
        byte? currencyId = null;

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            currencyId = await _db.Currency
                .Where(c => c.CurrencyCode == request.CurrencyCode)
                .Select(c => (byte?)c.CurrencyId)
                .SingleOrDefaultAsync(ct);
        }

        if (currencyId is null && partnerId is not null)
        {
            // Fallback: partner configuration currency (partner_configuration_id = 15)
            var partnerCurrencyCode = await _db.PartnerConfigurationPartner
                .Where(cp => cp.PartnerId == partnerId && cp.PartnerConfigurationId == PartnerCurrencyConfigId)
                .Select(cp => cp.ConfigurationValue)
                .SingleOrDefaultAsync(ct);

            if (!string.IsNullOrWhiteSpace(partnerCurrencyCode))
            {
                currencyId = await _db.Currency
                    .Where(c => c.CurrencyCode == partnerCurrencyCode)
                    .Select(c => (byte?)c.CurrencyId)
                    .SingleOrDefaultAsync(ct);
            }
        }

        currencyId ??= DefaultCurrencyId;

        // 2.1) Generate vendor_order_code if not supplied
        var vendorOrderCode = request.VendorOrderCode;
        if (string.IsNullOrWhiteSpace(vendorOrderCode))
        {
            var prefix = await _db.CartSiteIdOrderCodePrefix
                .Where(x => x.SiteId == request.SiteId)
                .Select(x => x.VendorOrderCodePrefix)
                .SingleOrDefaultAsync(ct) ?? string.Empty;

            var nextId = await GetNextVendorOrderIdAsync(ct);
            vendorOrderCode = $"{prefix}{nextId}";
        }

        // 2.2) Insert cart_order
        var order = new CartOrder
        {
            VendorOrderCode = vendorOrderCode,
            OrderType = request.SiteId,
            SiteId = request.SiteId,
            SiteUrl = request.SiteId,
            SalesOrderDate = salesOrderDate,
            SubmissionDate = now,
            Locale = request.Locale,
            UserIp = request.UserIp,
            CurrencyId = currencyId.Value,
            CartOrderStatusId = 1,  // 1 = pending/open; validated FK against cart_order_status
            OfferAmount = 0m,
            SubTotalAmount = 0m,
            TaxAmount = 0m,
            TotalAmount = 0m,
            CartCustomerId = 0,       // sentinel: no customer yet; set via usp_cart_update_cart_customer
            InvoiceInProcessId = 0,   // sentinel: payment workflow only
            PRc = request.PRc ?? string.Empty,
            PaymentMethod = "PENDING", // set at checkout time
            SessionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            InsertDate = now,
            ModifiedDate = now,
            InsertBy = request.AccountUserName
                       ?? request.CsiUserId?.ToString()
                       ?? "system",  // fallback until AuthMiddleware is wired
            ModifiedBy = request.AccountUserName
                         ?? request.CsiUserId?.ToString()
                         ?? "system" // fallback until AuthMiddleware is wired
        };

        _db.CartOrder.Add(order);
        await _db.SaveChangesAsync(ct);

        // 2.3) Insert cart_order_partner (optional)
        if (partnerId is not null)
        {
            int? partnerAccountId = null;

            if (!string.IsNullOrWhiteSpace(request.AccountUserName))
            {
                partnerAccountId = await (
                    from pa in _db.PartnerAccount
                    join a in _db.Account on pa.AccountId equals a.AccountId
                    where pa.PartnerId == partnerId && a.AccountUserName == request.AccountUserName
                    select (int?)pa.PartnerAccountId
                ).SingleOrDefaultAsync(ct);
            }

            _db.CartOrderPartner.Add(new CartOrderPartner
            {
                CartOrderId = order.CartOrderId,
                PartnerId = partnerId.Value,
                PartnerAccountId = partnerAccountId
            });
        }

        // 2.4) Insert cart_order_route (optional)
        if (!string.IsNullOrWhiteSpace(request.RoutingAction))
        {
            _db.CartOrderRoute.Add(new CartOrderRoute
            {
                CartOrderId = order.CartOrderId,
                RoutingAction = request.RoutingAction,
                InsertDate = now
            });
        }

        // 2.5) Insert cart_order_message (optional — when message_key is supplied)
        if (!string.IsNullOrWhiteSpace(request.MessageKey) && Guid.TryParse(request.MessageKey, out var messageKeyGuid))
        {
            var licenseId = await _db.LicenseKey
                .Where(k => k.Key == request.MessageKey)
                .Select(k => (int?)k.LicenseId)
                .SingleOrDefaultAsync(ct);

            _db.CartOrderMessage.Add(new CartOrderMessage
            {
                CartOrderId = order.CartOrderId,
                MessageKey = messageKeyGuid,
                MessageCampaignId = request.MessageCampaignId,
                MessageCampaignPlatform = request.MessageCampaignPlatform,
                CartDiscountId = request.CartDiscountId,
                LicenseId = licenseId,
                StatusId = 1  // 1 = active
            });
        }

        // 2.6) Insert cart_json (optional — stores the raw extension JSON)
        var cartExtensionJson = BuildCartExtensionJson(request);
        if (cartExtensionJson is not null)
        {
            _db.CartJson.Add(new CartJson
            {
                CartOrderId = order.CartOrderId,
                Json = cartExtensionJson
            });
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "InsertCartOrderHeaderAsync: created cart_order_id={CartOrderId} vendor_order_code={VendorOrderCode}",
            order.CartOrderId, vendorOrderCode);

        return vendorOrderCode;
    }

    // ── usp_cart_insert_cart_order_item ───────────────────────────────────────────

    public async Task InsertCartOrderItemAsync(
        int cartOrderId, string vendorOrderCode, CartOrderItemRequest item, int lineItem,
        CancellationToken ct = default)
    {
        // Look up license_category_id from license_category_name
        var licCategoryId = await _db.LicenseCategory
            .Where(lc => lc.LicenseCategoryName == item.LicenseCategoryName)
            .Select(lc => (int?)lc.LicenseCategoryId)
            .SingleOrDefaultAsync(ct);

        // ── GAP: usp_cart_insert_cart_order_item — sections 1–4 not yet ported ─────────────────
        //
        // The following logic from the stored procedure is NOT yet implemented here.
        // Each section lists the SQL objects it depends on and what C# work is required.
        //
        // SEC 1.2–1.3  LOCALE / LICENSE PROFILE
        //   Needs: fn_locale_to_lang_loc  → C# helper: split "en-US" into language_code + location_code
        //   Needs: entity License         → keycode → license_id, product_line_id, license_distribution_method_id
        //   Needs: entity LicenseMessage  → resolve next monthly process_date (message_type_id=10)
        //   Needs: fn_license_select_license_profile(license_id) → load renewal/upgrade license state
        //
        // SEC 1.9  PRODUCT LINE RESOLUTION
        //   Needs: entity LicenseCategoryProductLine
        //          → resolve product_line_id from language_code + location_code + license_category_id
        //   Needs: entity LicenseCategoryProductLineLicenseAttributeLicenseValue
        //          → resolve billing model (license_attribute_license_value) per location
        //   Needs: AppConfig key sets via fn_app_config_select_key_values:
        //          PILLR_LICENSE_CATEGORIES, UTILITY_BILLING_MODELS, CARBONITE_LICENSE_CATEGORIES,
        //          BUSINESS_PRODUCT_LINE, DEFAULT_BUSINESS_BILLING_MODEL
        //
        // SEC 1.12–1.14  USAGE MODEL / RETENTION / PLATFORM OVERRIDES
        //   Needs: entity PartnerUsagePricingModel  → per-partner usage_pricing_model_id override
        //   Needs: entity PartnerRetentionModel     → per-partner retention_model_id override
        //   Needs: entity PartnerProductPlatform    → per-partner product_platform_id override
        //
        // SEC 1.15  STORAGE
        //   Needs: fn_get_item_storage_gb(...) → C# helper or DB scalar to derive storage_gb when null
        //
        // SEC 2.1–2.5  DATE & PRODUCT-TYPE DERIVATION
        //   start_date / expiration_date calculated from license profile + years + billing model
        //   product_type_id determined as 1=new / 2=renewal / 3=upgrade
        //   Upgrade item injection (delta-seat rows) added to item set
        //   Needs: fn_product_select_profile(...) → resolve product_id from 12 attributes
        //          (product_line_id, license_category_id, years, qty, storage_gb, contract_days,
        //           product_type_id, license_keycode_type_id, usage_pricing_model_id,
        //           retention_model_id, product_platform_id, sap_material_number)
        //
        // SEC 3.1  CONSUMER PRODUCT SET & PRICING
        //   Needs: usp_cart_select_renewal_product_set → returns retail_price + upgrade_price per item
        //   Needs: entity ProductStorage               → storage products with storage_gb
        //   Discount logic applied from cart_discount_method_id:
        //     method 3 = percentage off retail_price
        //     method 1 = fixed amount off retail_price
        //     else      = upgrade_price
        //
        // SEC 4.1  BUSINESS DIRECT PRICING
        //   list_price = product_pricing.retail_price (Pillr utility → $0)
        //   unit_price = pro-rated by contract days / capability_activation_days (new/renewal)
        //              = pro-rated by contract days / upgrade_days (upgrade)
        //   Needs: entity ProductCapability           → capability_activation_days
        //   Needs: fn_leap_days_between(start, end)   → C# helper: count Feb-29 occurrences in range
        //   Tier discount via usp_license_select_category_discount_model:
        //     Needs: result mapped into @item_discount_profile, applied as % off unit_price
        //   Needs: entities LicenseCartDiscount + CartDiscountItem → apply license_cart_discount
        // ─────────────────────────────────────────────────────────────────────────────────────────

        // Resolve unit_price: use override from request if present,
        // otherwise fall back to product_pricing.retail_price (no pro-rating, no partner/tier discount).
        decimal? unitPrice = item.UnitPrice;
        decimal? listPrice = null;

        if (unitPrice is null)
        {
            listPrice = await _db.ProductPricing
                .Where(pp => pp.ProductId == item.ProductId)
                .Select(pp => (decimal?)pp.RetailPrice)
                .FirstOrDefaultAsync(ct);
            unitPrice = listPrice;
        }

        var insertBy = "system";  // insert identity — no auth context at item scope; set by caller if needed
        var now2 = DateTime.UtcNow;

        var cartItem = new CartOrderItem
        {
            CartOrderId = cartOrderId,
            LineItem = lineItem,
            ProductId = item.ProductId,
            Quantity = item.Quantity ?? 1,
            StorageGb = item.StorageGb,
            ListPrice = listPrice ?? 0m,       // NOT NULL; default 0 when no pricing row found
            UnitPrice = unitPrice ?? 0m,       // NOT NULL; default 0 when no pricing row found
            UnitPricePreVat = null,  // not provided at item level; set downstream when VAT is calculated
            TaxItemTotal = 0m,
            TaxExempt = false,
            VendorId = 1,                      // 1 = Webroot default
            InvoiceItemInProcessId = 0,        // sentinel: payment workflow only
            UsagePrice = item.UsagePrice,
            StartDate = item.StartDate,
            ExpirationDate = item.ExpirationDate,
            CartItemBundleId = item.CartItemBundleId,
            ItemHierarchyId = item.ItemHierarchyId.HasValue ? (byte?)item.ItemHierarchyId.Value : null,
            LicenseAttributeLicenseValue = item.LicenseAttributeLicenseValue,
            VendorOrderItemCode = item.VendorOrderItemCode,
            Discount = item.Discount,
            CartDiscountMethodId = item.CartDiscountMethodId,
            CartDiscountId = item.CartDiscountId,
            OpportunityLineItemId = item.OpportunityLineItemId,
            ProductLocale = item.Locale,
            CartOrderStatusId = 1,  // 1 = pending/open
            InsertDate = now2,
            ModifiedDate = now2,
            InsertBy = insertBy,
            ModifiedBy = insertBy
        };

        _db.CartOrderItem.Add(cartItem);
        await _db.SaveChangesAsync(ct);

        // Insert cart_order_item_json for extended fields (vault, platform, retention, pricing level)
        var itemJson = BuildCartOrderItemJson(item);
        if (itemJson is not null)
        {
            _db.CartOrderItemJson.Add(new CartOrderItemJson
            {
                CartOrderItemId = cartItem.CartOrderItemId,
                Json = itemJson,
                InsertDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        _logger.LogInformation(
            "InsertCartOrderItemAsync: created cart_order_item_id={CartOrderItemId} line={LineItem} product={ProductId}",
            cartItem.CartOrderItemId, lineItem, item.ProductId);
    }

    // ── usp_cart_select_cart_order ────────────────────────────────────────────────

    public async Task<CartOrderResponse?> SelectCartOrderHeaderAsync(
        string vendorOrderCode, CancellationToken ct = default)
    {
        var row = await (
            from co in _db.CartOrder
            join cu in _db.Currency on co.CurrencyId equals cu.CurrencyId
            join cp in _db.CartOrderPartner on co.CartOrderId equals cp.CartOrderId into cpJoin
            from cp in cpJoin.DefaultIfEmpty()
            join p in _db.Partner on cp.PartnerId equals p.PartnerId into pJoin
            from p in pJoin.DefaultIfEmpty()
            join j in _db.CartJson on co.CartOrderId equals j.CartOrderId into jJoin
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
                co.CurrencyId,
                cu.CurrencyCode,
                co.UserIp,
                PartnerKey = p != null ? p.PartnerKey.ToString() : null,
                CartJson = j != null ? j.Json : null
            }
        ).SingleOrDefaultAsync(ct);

        if (row is null) return null;

        return new CartOrderResponse
        {
            CartOrderId = row.CartOrderId,
            VendorOrderCode = row.VendorOrderCode,
            SiteId = row.SiteId,
            OfferAmount = row.OfferAmount,
            TotalAmount = row.TotalAmount,
            SubTotalAmount = row.SubTotalAmount,
            TaxAmount = row.TaxAmount,
            SalesOrderDate = row.SalesOrderDate,
            Locale = row.Locale,
            InsertDate = row.InsertDate,
            InsertBy = row.InsertBy,
            ModifiedDate = row.ModifiedDate,
            ModifiedBy = row.ModifiedBy,
            CartOrderStatusId = row.CartOrderStatusId,
            CurrencyId = (int)row.CurrencyId,
            CurrencyCode = row.CurrencyCode,
            UserIp = row.UserIp,
            PartnerKey = row.PartnerKey,
            CartJson = row.CartJson,
            Items = new()
        };
    }

    // ── usp_cart_select_cart_order_item ───────────────────────────────────────────

    public async Task<List<CartOrderItemResponse>> SelectCartOrderItemsAsync(
        string vendorOrderCode, CancellationToken ct = default)
    {
        // Resolve cart_order_id + cart locale from vendor_order_code
        var orderInfo = await _db.CartOrder
            .Where(o => o.VendorOrderCode == vendorOrderCode)
            .Select(o => new { o.CartOrderId, o.Locale })
            .FirstOrDefaultAsync(ct);

        if (orderInfo is null) return [];

        // Main item query — mirrors the FROM/JOIN structure in usp_cart_select_cart_order_item
        var rows = await (
            from i in _db.CartOrderItem
            join p in _db.Product on i.ProductId equals p.ProductId
            join pf in _db.ProductFamily on p.ProductFamilyId equals pf.ProductFamilyId into pfJoin
            from pf in pfJoin.DefaultIfEmpty()
            join plp in _db.ProductLineProduct on p.ProductId equals plp.ProductId into plpJoin
            from plp in plpJoin.DefaultIfEmpty()
            join prl in _db.ProductLine on (plp != null ? plp.ProductLineId : -1) equals prl.ProductLineId into prlJoin
            from prl in prlJoin.DefaultIfEmpty()
            join t in _db.ProductType on p.ProductTypeId equals t.ProductTypeId into tJoin
            from t in tJoin.DefaultIfEmpty()
            join ij in _db.CartOrderItemJson on i.CartOrderItemId equals ij.CartOrderItemId into ijJoin
            from ij in ijJoin.DefaultIfEmpty()
            join plc in _db.ProductLicenseCategory on p.ProductId equals plc.ProductId into plcJoin
            from plc in plcJoin.DefaultIfEmpty()
            join lc in _db.LicenseCategory on plc.LicenseCategoryId equals lc.LicenseCategoryId into lcJoin
            from lc in lcJoin.DefaultIfEmpty()
            join kt in _db.LicenseKeycodeType on p.LicenseKeycodeTypeId equals kt.LicenseKeycodeTypeId into ktJoin
            from kt in ktJoin.DefaultIfEmpty()
            join y in _db.ProductYears on p.ProductId equals y.ProductId into yJoin
            from y in yJoin.DefaultIfEmpty()
            join s in _db.ProductSeat on p.ProductId equals s.ProductId into sJoin
            from s in sJoin.DefaultIfEmpty()
            join v in _db.LicenseAttributeLicenseValue
                on i.LicenseAttributeLicenseValue equals v.Value into vJoin
            from v in vJoin.DefaultIfEmpty()
            join il in _db.CartOrderItemLicense on i.CartOrderItemId equals il.CartOrderItemId into ilJoin
            from il in ilJoin.DefaultIfEmpty()
            where i.CartOrderId == orderInfo.CartOrderId
            select new
            {
                // cart_order_item fields
                i.CartOrderItemId,
                i.CartOrderId,
                i.LineItem,
                i.Quantity,
                i.StorageGb,
                i.OrderItemOfferAmount,
                i.ListPrice,
                i.UnitPrice,
                i.UnitPricePreVat,
                i.TaxItemTotal,
                i.UsagePrice,
                i.ProductId,
                i.StartDate,
                i.ExpirationDate,
                i.CartItemBundleId,
                i.ItemHierarchyId,
                i.LicenseAttributeLicenseValue,
                i.VendorOrderItemCode,
                i.OrderItemUpdateTypeId,
                i.Discount,
                i.CartDiscountMethodId,
                i.CartDiscountId,
                i.OpportunityLineItemId,
                // product
                p.ProductDescription,
                p.LicenseKeycodeTypeId,
                // product_type (LEFT JOIN — null safe)
                ProductTypeId = t != null ? (int?)t.ProductTypeId : null,
                ProductTypeDescription = t != null ? t.ProductTypeDescription : null,
                // product_family (LEFT JOIN — null safe)
                ProductFamilyDescription = pf != null ? pf.ProductFamilyDescription : null,
                // product_line (LEFT JOIN — null safe)
                ProductLineCartType = prl != null ? prl.ProductLineCartType : null,
                // license_keycode_type
                LicenseKeycodeTypeDescription = kt != null ? kt.LicenseKeycodeTypeDescription : null,
                // license_category
                LicenseCategoryId = lc != null ? (int?)lc.LicenseCategoryId : null,
                LicenseCategoryName = lc != null ? lc.LicenseCategoryName : null,
                LicenseCategoryDescription = lc != null ? lc.LicenseCategoryDescription : null,
                MinOrderQuantity = lc != null ? lc.MinOrderQuantity : null,
                MaxOrderQuantity = lc != null ? lc.MaxOrderQuantity : null,
                // product_years
                Years = y != null ? (double?)y.Years : null,
                // product_seat
                Seats = s != null ? (int?)s.Seats : null,
                // license_attribute_license_value
                LicenseAttributeLicenseValueDescription = v != null ? v.Description : null,
                // cart_order_item_license
                Keycode = il != null ? il.Keycode : null,
                // cart_order_item_json (raw, will be parsed client-side)
                ItemJsonRaw = ij != null ? ij.Json : null
            }
        ).ToListAsync(ct);

        // Resolve dependent item IDs (self-join for hierarchy 2 → hierarchy 1 parent)
        // Mirrors: LEFT JOIN (...subquery...) d ON d.cart_order_id = i.cart_order_id AND ...
        var dependentMap = await BuildDependentItemMapAsync(orderInfo.CartOrderId, ct);

        return rows.Select(r =>
        {
            var jp = ParseCartOrderItemJson(r.ItemJsonRaw);

            // ── GAP: equivalent_year_price — fn_cart_select_one_year_products + fn_locale_to_lang_loc not yet ported ──
            //
            // The SP computes equivalent_year_price with:
            //   OUTER APPLY fn_locale_to_lang_loc(ISNULL(i.product_locale, @cart_locale)) ll
            //   OUTER APPLY fn_cart_select_one_year_products(p.product_id) oyp
            //   LEFT JOIN product_pricing pp
            //       ON pp.product_id = oyp.product_id
            //      AND pp.location_code = ll.location_code
            //      AND pp.language_code = ll.language_code
            //
            // What is needed to fix this:
            //   1. fn_locale_to_lang_loc: C# helper — split "en-US" into language_code="en", location_code="US"
            //      (no new entity required; pure string logic)
            //   2. fn_cart_select_one_year_products: resolve the 1-year product variant for a given product_id
            //      Needs: product_pricing joined with product_years where years = 1
            //      (may require a new helper or additional ProductPricing query)
            //   3. Replace the current unit_price * years proxy below with:
            //      retail_price of the 1-year equivalent product in the item/cart locale
            // ──────────────────────────────────────────────────────────────────────────────────────────────────────
            decimal? equivalentYearPrice = null;
            if (r.Years.HasValue)
            {
                var isUsagePricingModel2 = jp?.UsagePricingModelId == 2;
                var itemTotalNull = jp?.ItemTotal is null;
                var storageLessThan1TB = (r.StorageGb ?? 0) < 1024;

                if (!(isUsagePricingModel2 && itemTotalNull && storageLessThan1TB))
                {
                    equivalentYearPrice = r.UnitPrice * (decimal)r.Years.Value;
                }
            }

            var dependentId = dependentMap.TryGetValue(
                (r.CartOrderId, r.CartItemBundleId, r.LineItem), out var d) ? d : (int?)null;

            return new CartOrderItemResponse
            {
                CartOrderItemId = r.CartOrderItemId,
                CartOrderId = r.CartOrderId,
                LineItem = r.LineItem,
                Quantity = r.Quantity,
                StorageGb = r.StorageGb,
                Years = r.Years.HasValue ? (decimal?)r.Years.Value : null,
                OrderItemOfferAmount = r.OrderItemOfferAmount,
                EquivalentYearPrice = equivalentYearPrice,
                ListPrice = r.ListPrice,
                UnitPrice = r.UnitPrice,
                UnitPricePreVat = r.UnitPricePreVat,
                TaxItemTotal = r.TaxItemTotal,
                UsagePrice = r.UsagePrice,
                ProductId = r.ProductId,
                ProductDescription = r.ProductDescription,
                ProductTypeId = r.ProductTypeId,
                ProductTypeDescription = r.ProductTypeDescription,
                LicenseKeycodeTypeId = r.LicenseKeycodeTypeId,
                LicenseKeycodeTypeDescription = r.LicenseKeycodeTypeDescription,
                LicenseCategoryId = r.LicenseCategoryId,
                LicenseCategoryName = r.LicenseCategoryName,
                LicenseCategoryDescription = r.LicenseCategoryDescription,
                ProductFamilyDescription = r.ProductFamilyDescription,
                ProductLineCartType = r.ProductLineCartType,
                MinOrderQuantity = r.MinOrderQuantity,
                MaxOrderQuantity = r.MaxOrderQuantity,
                StartDate = r.StartDate,
                ExpirationDate = r.ExpirationDate,
                CartItemBundleId = r.CartItemBundleId,
                ItemHierarchyId = r.ItemHierarchyId,
                DependentCartOrderItemId = dependentId,
                Keycode = r.Keycode,
                LicenseAttributeLicenseValue = r.LicenseAttributeLicenseValue,
                LicenseAttributeLicenseValueDescription = r.LicenseAttributeLicenseValueDescription,
                VendorOrderItemCode = r.VendorOrderItemCode,
                OrderItemUpdateTypeId = r.OrderItemUpdateTypeId,
                Discount = r.Discount,
                CartDiscountMethodId = r.CartDiscountMethodId,
                CartDiscountId = r.CartDiscountId,
                OpportunityLineItemId = r.OpportunityLineItemId,
                // JSON-derived fields (from fn_cart_select_cart_order_item_json equivalent)
                UsagePricingModelId = jp?.UsagePricingModelId,
                UsagePricingModelName = jp?.UsagePricingModelName,
                RetentionModelId = jp?.RetentionModelId,
                RetentionModelName = jp?.RetentionModelName,
                RetentionTerm = jp?.RetentionTerm,
                RetentionModelTypeId = jp?.RetentionModelTypeId,
                ProductPlatformId = jp?.ProductPlatformId,
                ProductPlatformName = jp?.ProductPlatformName,
                VaultId = jp?.VaultId,
                VaultDatacenterName = jp?.VaultDatacenterName,
                Vault = jp?.Vault,
                ProductPricingLevelId = jp?.ProductPricingLevelId,
                PricingLevelDescription = jp?.PricingLevelDescription,
                LicenseSeats = r.Seats
            };
        }).ToList();
    }

    // ── Quote-key check ───────────────────────────────────────────────────────────

    public async Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key, CancellationToken ct = default)
    {
        if (!Guid.TryParse(key, out var keyGuid)) return null;
        return await (
            from m in _db.CartOrderMessage
            join o in _db.CartOrder on m.CartOrderId equals o.CartOrderId
            where m.MessageKey == keyGuid
            select o.VendorOrderCode
        ).FirstOrDefaultAsync(ct);
    }

    // ── Read-path stubs (implemented by downstream services in future) ────────────

    private async Task<Guid?> GetOrderMessageKeyAsync(int cartOrderId, CancellationToken ct)
        => await _db.CartOrderMessage
            .Where(m => m.CartOrderId == cartOrderId)
            .Select(m => (Guid?)m.MessageKey)
            .FirstOrDefaultAsync(ct);

    private static string GetCurrencySymbol(string? currencyCode) => currencyCode switch
    {
        "EUR" => "€",
        "GBP" => "£",
        "CAD" => "C$",
        "AUD" => "A$",
        _ => "$"   // USD and all other codes default to $
    };

    private static string? FormatCurrency(decimal? value, string symbol)
        => value.HasValue
            ? $"{symbol}{value.Value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)}"
            : null;

    public Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
        string keycode, CancellationToken ct = default)
        => Task.FromResult<LicenseOptionsResponse?>(null);

    public Task<ConfigureResponse?> SelectConfigureAsync(
        string keycode, CancellationToken ct = default)
        => Task.FromResult<ConfigureResponse?>(null);

    public Task<UpgradeResponse?> SelectUpgradeAsync(
        string keycode, CancellationToken ct = default)
        => Task.FromResult<UpgradeResponse?>(null);

    // ── Private helpers ───────────────────────────────────────────────────────────

    /// <summary>
    /// Generates the next sequential vendor order ID — mirrors the ids-table path of usp_next_id @Type=3.
    /// <para>
    /// The SP increments <c>ids.next_id</c> where <c>id_type = 3</c> and returns the new value.
    /// <c>ExecuteUpdateAsync</c> emits a single <c>UPDATE</c> statement that acquires an X lock on
    /// the row; concurrent callers serialise on that lock, matching the SP's <c>BEGIN TRANSACTION</c>
    /// behaviour.
    /// </para>
    /// <para>
    /// Note: the SP also has an <c>Invoices_sequence</c> identity-table path that is activated only
    /// in production environments where that table exists and is seeded. That path is not ported here;
    /// the ids-table path covers local and QA environments.
    /// </para>
    /// </summary>
    private async Task<int> GetNextVendorOrderIdAsync(CancellationToken ct)
    {
        // Atomically increment — the UPDATE X lock prevents concurrent callers from
        // reading the same next_id value before this transaction commits.
        await _db.Ids
            .Where(r => r.IdType == 3)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.NextId, r => r.NextId + 1)
                .SetProperty(r => r.LastModified, _ => DateTime.UtcNow), ct);

        return await _db.Ids
            .Where(r => r.IdType == 3)
            .Select(r => r.NextId)
            .SingleAsync(ct);
    }

    /// <summary>
    /// Builds the dependent-item map used to resolve DependentCartOrderItemId.
    /// Mirrors the self-join subquery in usp_cart_select_cart_order_item.
    /// </summary>
    private async Task<Dictionary<(int CartOrderId, int? BundleId, int LineItem), int>> BuildDependentItemMapAsync(
        int cartOrderId, CancellationToken ct)
    {
        var primaryItems = await (
            from i in _db.CartOrderItem
            join p in _db.Product on i.ProductId equals p.ProductId
            join t in _db.ProductType on p.ProductTypeId equals t.ProductTypeId
            where i.CartOrderId == cartOrderId && (t.ProductTypeId == 1 || t.ProductTypeId == 2)
            select new { i.CartOrderItemId, i.CartOrderId, i.LineItem, i.CartItemBundleId }
        ).ToListAsync(ct);

        // Map: (cartOrderId, bundleId, hierarchyLineItem) → primaryItemId
        return primaryItems.ToDictionary(
            x => (x.CartOrderId, x.CartItemBundleId, x.LineItem),
            x => x.CartOrderItemId);
    }

    /// <summary>
    /// Serializes the cart extension JSON blob for cart_json.insert (SP section 2.6).
    /// Returns null when there is nothing worth storing.
    /// </summary>
    private static string? BuildCartExtensionJson(CartOrderCreateRequest r)
    {
        // Only store if at least one optional extension field was provided
        if (r.CurrencyCode is null && r.PartnerKey is null
            && r.AccountUserName is null && r.RoutingAction is null && r.MessageKey is null
            && r.MessageCampaignId is null && r.MessageCampaignPlatform is null
            && r.CartDiscountId is null && r.SalesOrderDate is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new
        {
            currency_code = r.CurrencyCode,
            partner_key = r.PartnerKey,
            account_user_name = r.AccountUserName,
            routing_action = r.RoutingAction,
            sales_order_date = r.SalesOrderDate,
            message_campaign_id = r.MessageCampaignId,
            message_campaign_platform = r.MessageCampaignPlatform,
            key = r.MessageKey,
            cart_discount_id = r.CartDiscountId
        });
    }

    /// <summary>
    /// Serializes per-item extended fields into cart_order_item_json.
    /// This is the equivalent of the fn_cart_select_cart_order_item_json TVF input.
    /// Returns null when no extended fields are present.
    /// </summary>
    private static string? BuildCartOrderItemJson(CartOrderItemRequest item)
    {
        if (item.UsagePricingModelId is null && item.RetentionModelId is null
            && item.ProductPlatformId is null && item.VaultId is null
            && item.ProductPricingLevelId is null && item.ItemTotal is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(new
        {
            usage_pricing_model_id = item.UsagePricingModelId,
            retention_model_id = item.RetentionModelId,
            product_platform_id = item.ProductPlatformId,
            vault_id = item.VaultId,
            product_pricing_level_id = item.ProductPricingLevelId,
            item_total = item.ItemTotal
        });
    }

    /// <summary>
    /// Parses cart_order_item_json into a typed DTO.
    /// Equivalent to fn_cart_select_cart_order_item_json OUTER APPLY in the select SP.
    /// </summary>
    private static CartOrderItemJsonDto? ParseCartOrderItemJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<CartOrderItemJsonDto>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    // ── Internal DTO for cart_order_item_json parsing ─────────────────────────────

    private sealed class CartOrderItemJsonDto
    {
        public int? UsagePricingModelId { get; init; }
        public string? UsagePricingModelName { get; init; }
        public int? RetentionModelId { get; init; }
        public string? RetentionModelName { get; init; }
        public int? RetentionTerm { get; init; }
        public int? RetentionModelTypeId { get; init; }
        public int? ProductPlatformId { get; init; }
        public string? ProductPlatformName { get; init; }
        public int? VaultId { get; init; }
        public string? VaultDatacenterName { get; init; }
        public string? Vault { get; init; }
        public int? ProductPricingLevelId { get; init; }
        public string? PricingLevelDescription { get; init; }
        public decimal? ItemTotal { get; init; }
    }
}
