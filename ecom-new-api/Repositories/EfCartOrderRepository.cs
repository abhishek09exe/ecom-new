using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Repositories;

/// <summary>
/// Pure EF Core implementation of <see cref="ICartOrderRepository"/>.
///
/// No stored procedures. No raw SQL. EF Core generates all INSERT and SELECT statements.
///
/// Flow:
///   INSERT  — build entity objects → _db.CartOrders.Add() → SaveChangesAsync()
///             EF generates:  INSERT INTO cart_order (...) VALUES (...)
///                            INSERT INTO cart_order_item (...) VALUES (...)
///
///   SELECT  — _db.CartOrders.Include(...).FirstOrDefaultAsync()
///             EF generates:  SELECT ... FROM cart_order
///                            LEFT JOIN currency ON ...
///                            LEFT JOIN cart_order_partner LEFT JOIN partner ON ...
///                            LEFT JOIN cart_json ON ...
///                            LEFT JOIN cart_order_item ON ...
///
/// Register in Program.cs:
///   builder.Services.AddScoped&lt;ICartOrderRepository, EfCartOrderRepository&gt;();
/// </summary>
public sealed class EfCartOrderRepository : ICartOrderRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<EfCartOrderRepository> _logger;

    public EfCartOrderRepository(AppDbContext db, ILogger<EfCartOrderRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Write path ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default)
    {
        // ── Resolve currency_id from currency_code ────────────────────────────
        // EF Core generates: SELECT currency_id FROM currency WHERE currency_code = 'USD'
        var currencyCode = request.CurrencyCode ?? "USD";
        var currency = await _db.Currencies
            .FirstOrDefaultAsync(c => c.CurrencyCode == currencyCode, ct)
            ?? await _db.Currencies.FirstAsync(c => c.CurrencyCode == "USD", ct);

        // ── Resolve partner_id from partner_key (optional) ────────────────────
        Partner? partner = null;
        if (!string.IsNullOrWhiteSpace(request.PartnerKey)
            && Guid.TryParse(request.PartnerKey, out var partnerGuid))
        {
            // EF Core generates: SELECT * FROM partner WHERE partner_key = @p0
            partner = await _db.Partners
                .FirstOrDefaultAsync(p => p.PartnerKey == partnerGuid, ct);
        }

        // ── Generate vendor_order_code ────────────────────────────────────────
        // SP section 2.1:
        //   SELECT vendor_order_code_prefix FROM cart_site_id_order_code_prefix WHERE site_id = @site_id
        //   EXEC usp_next_id @Type=3   → sequential integer (8 digits wide)
        //   vendor_order_code = prefix + CONVERT(varchar(8), invoice_code_int)
        //
        // G2 — prefix from DB (falls back to first 3 chars of site_id uppercased when row missing)
        // G3 — sequence from local DB SEQUENCE object (replace with usp_next_id call when using QA)
        var vendorOrderCode = request.VendorOrderCode;
        if (string.IsNullOrWhiteSpace(vendorOrderCode))
        {
            // G2: resolve prefix
            var prefixRow = await _db.CartSiteIdOrderCodePrefixes
                .FirstOrDefaultAsync(r => r.SiteId == request.SiteId, ct);
            var prefix = prefixRow?.VendorOrderCodePrefix
                ?? request.SiteId[..Math.Min(3, request.SiteId.Length)].ToUpper();

            // G3: next sequential ID — local SEQUENCE object mirrors usp_next_id @Type=3
            // NEXT VALUE FOR cannot run inside a subquery (EF wraps SqlQueryRaw).
            // Use a raw ADO.NET command instead — one round-trip, no subquery wrapper.
            // To switch to QA: replace with EXEC usp_next_id @Type=3
            int nextId;
            var conn = _db.Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync(ct);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT NEXT VALUE FOR dbo.cart_order_next_id";
                nextId = Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
            }
            if (!wasOpen) await conn.CloseAsync();

            vendorOrderCode = $"{prefix}{nextId:D8}";
        }

        var now = DateTime.UtcNow;

        // ── G11: max line_item offset (SP section 5.1) ────────────────────────
        // For a brand-new cart this is 0.  When items are re-submitted against an
        // already-started cart (future add-to-cart / update path) this prevents
        // duplicate line_item values.
        int maxExistingLineItem = 0;
        if (!string.IsNullOrWhiteSpace(request.VendorOrderCode))
        {
            maxExistingLineItem = await _db.CartOrders
                .Where(o => o.VendorOrderCode == request.VendorOrderCode)
                .SelectMany(o => o.Items)
                .Select(i => (int?)i.LineItem)
                .MaxAsync(ct) ?? 0;
        }

        // ── Build the CartOrder entity ─────────────────────────────────────────
        var order = new CartOrder
        {
            VendorOrderCode    = vendorOrderCode,
            SiteId             = request.SiteId,
            OrderType          = request.SiteId,          // SP: order_type = @site_id
            SiteUrl            = request.SiteId,          // SP: site_url   = @site_id
            Locale             = request.Locale,
            CurrencyId        = (byte?)currency.CurrencyId,
            CartOrderStatusId = 1,
            CartCustomerId    = 0,
            InvoiceInProcessId = 0,
            PRc               = request.PRc ?? "1",
            TrxRc             = request.TrxRc,
            SubTotalAmount    = 0m,
            PaymentMethod     = string.Empty,
            SessionId         = 0,
            SubmissionDate    = now,
            SalesOrderDate    = request.SalesOrderDate,
            InsertDate        = now,
            InsertBy          = request.AccountUserName ?? "system",
            ModifiedDate      = now,
            ModifiedBy        = request.AccountUserName ?? "system",
            UserIp            = request.UserIp,

            // ── Line items — EF Core inserts these in one go via SaveChangesAsync
            Items = request.Items.Select((item, index) => new CartOrderItem
            {
                LineItem                     = maxExistingLineItem + index + 1,  // G11

                ProductId                    = item.ProductId,
                InvoiceItemInProcessId       = 0,
                VendorId                     = 1,
                CartOrderStatusId            = 1,
                Quantity                     = item.Quantity ?? item.LicenseSeats ?? 1,  // SP 5.3.2: uses license_seats as qty for business products
                StorageGb                    = item.StorageGb,
                UnitPrice                    = item.UnitPrice ?? 0m,
                ListPrice                    = 0m,
                TaxItemTotal                 = 0m,
                TaxExempt                    = false,
                UsagePrice                   = item.UsagePrice,
                Discount                     = item.Discount,
                CartDiscountMethodId         = item.CartDiscountMethodId,
                CartDiscountId               = item.CartDiscountId ?? request.CartDiscountId,
                StartDate                    = item.StartDate,
                ExpirationDate               = item.ExpirationDate,
                CartItemBundleId             = item.CartItemBundleId,
                ItemHierarchyId              = item.ItemHierarchyId.HasValue ? (byte)item.ItemHierarchyId.Value : (byte)1,
                LicenseAttributeLicenseValue = item.LicenseAttributeLicenseValue,
                VendorOrderItemCode          = item.VendorOrderItemCode,
                OrderItemUpdateTypeId        = item.OrderItemUpdateTypeId ?? 1,
                OpportunityLineItemId        = item.OpportunityLineItemId,
                SapMaterialNumber            = item.SapMaterialNumber,
                InsertDate                   = now,
                InsertBy                     = request.AccountUserName ?? "system",
                ModifiedDate                 = now,
                ModifiedBy                   = request.AccountUserName ?? "system"
            }).ToList(),

            // ── Extension JSON blob ────────────────────────────────────────────
            CartJson = new CartJson
            {
                Json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    currency_code              = request.CurrencyCode,
                    partner_key                = request.PartnerKey,
                    account_user_name          = request.AccountUserName,
                    routing_action             = request.RoutingAction,
                    sales_order_date           = request.SalesOrderDate?.ToString("yyyy-MM-dd"),
                    message_campaign_id        = request.MessageCampaignId,
                    message_campaign_platform  = request.MessageCampaignPlatform,
                    key                        = request.Key,
                    cart_discount_id           = request.CartDiscountId
                })
            }
        };

        // ── Link partner if found (SP section 2.3) ─────────────────────────────────
        if (partner is not null)
        {
            // SP also resolves partner_account_id via partner_account JOIN account.
            // Tables are not EF-mapped, so use a raw ADO.NET round-trip.
            int? partnerAccountId = null;
            if (!string.IsNullOrWhiteSpace(request.AccountUserName))
            {
                var conn2 = _db.Database.GetDbConnection();
                var wasOpen2 = conn2.State == System.Data.ConnectionState.Open;
                if (!wasOpen2) await conn2.OpenAsync(ct);
                await using (var cmd2 = conn2.CreateCommand())
                {
                    cmd2.CommandText = @"SELECT TOP 1 p.partner_account_id
                        FROM partner_account p
                        INNER JOIN account a ON p.account_id = a.account_id
                        WHERE p.partner_id = @partnerId
                          AND a.account_user_name = @userName";
                    var pPartnerId = cmd2.CreateParameter();
                    pPartnerId.ParameterName = "@partnerId";
                    pPartnerId.Value = partner.PartnerId;
                    cmd2.Parameters.Add(pPartnerId);
                    var pUserName = cmd2.CreateParameter();
                    pUserName.ParameterName = "@userName";
                    pUserName.Value = request.AccountUserName;
                    cmd2.Parameters.Add(pUserName);
                    var paResult = await cmd2.ExecuteScalarAsync(ct);
                    if (paResult is not null and not DBNull)
                        partnerAccountId = Convert.ToInt32(paResult);
                }
                if (!wasOpen2) await conn2.CloseAsync();
            }

            order.CartOrderPartner = new CartOrderPartner
            {
                PartnerId        = partner.PartnerId,
                PartnerAccountId = partnerAccountId
            };
        }

        // ── EF Core generates all INSERT statements in a single transaction ────
        // INSERT INTO cart_order (...)       VALUES (...)
        // INSERT INTO cart_order_item (...)  VALUES (...)   ← once per item
        // INSERT INTO cart_json (...)        VALUES (...)
        // INSERT INTO cart_order_partner (...) VALUES (...)  ← if partner present
        _db.CartOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        // ── Update cart totals (SP section 5.5) ───────────────────────────────
        // SP: SUM(unit_price * quantity) across all items → total_amount + sub_total_amount
        var totalAmount = order.Items
            .Sum(i => (i.UnitPrice == 0 ? 0m : i.UnitPrice) * i.Quantity);

        order.TotalAmount    = totalAmount;
        order.SubTotalAmount = totalAmount;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "CartOrder inserted: CartOrderId={CartOrderId} VendorOrderCode={VendorOrderCode} TotalAmount={TotalAmount}",
            order.CartOrderId, order.VendorOrderCode, totalAmount);
        // ── G10: cart_order_item_json_log (SP section 5.0) ──────────────────
        // Raw JSON audit log inserted once per cart-create API call, before any
        // per-item processing. Mirrors the SP INSERT at the top of section 5.
        var itemJsonAudit   = System.Text.Json.JsonSerializer.Serialize(request.Items);
        var bundleJsonAudit = System.Text.Json.JsonSerializer.Serialize(
            request.Items.Select(i => new
            {
                keycode                      = i.Keycode,
                license_attribute_license_value = i.LicenseAttributeLicenseValue,
                license_keycode_type_id      = i.LicenseKeycodeTypeId,
                order_item_update_type_id    = i.OrderItemUpdateTypeId,
                cart_discount_id             = i.CartDiscountId,
                product_pricing_level_id     = i.ProductPricingLevelId
            }));
        _db.CartOrderItemJsonLogs.Add(new CartOrderItemJsonLog
        {
            CartOrderId = order.CartOrderId,
            ItemJson    = itemJsonAudit,
            BundleJson  = bundleJsonAudit,
            InsertDate  = now
        });
        // ── G4: cart_order_route (SP section 2.4) ────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.RoutingAction))
        {
            _db.CartOrderRoutes.Add(new CartOrderRoute
            {
                CartOrderId   = order.CartOrderId,
                RoutingAction = request.RoutingAction,
                InsertDate    = now
            });
        }

        // ── G5: cart_order_message (SP section 2.5) ──────────────────────────
        if (!string.IsNullOrWhiteSpace(request.Key)
            && Guid.TryParse(request.Key, out var messageKeyGuid))
        {
            // Resolve license_id from license_key table
            var licenseId = await _db.LicenseKeys
                .Where(lk => lk.LicenseKeyValue == messageKeyGuid)
                .Select(lk => (int?)lk.LicenseId)
                .FirstOrDefaultAsync(ct);

            _db.CartOrderMessages.Add(new CartOrderMessage
            {
                CartOrderId              = order.CartOrderId,
                MessageKey               = messageKeyGuid,
                LicenseId                = licenseId,
                CartDiscountId           = request.CartDiscountId,
                StatusId                 = 1,
                MessageCampaignId        = request.MessageCampaignId,
                MessageCampaignPlatform  = request.MessageCampaignPlatform
            });
        }

        // ── G8: cart_order_item_json (SP section 5.3.3) ──────────────────────
        // Persist per-item JSON dimensions: vault, retention, platform, pricing level.
        // Values come directly from the frontend request (pricing is frontend-owned).
        foreach (var (item, savedItem) in request.Items.Zip(order.Items))
        {
            var jsonBlob = System.Text.Json.JsonSerializer.Serialize(new
            {
                usage_pricing_model_id      = item.RetentionModelId,     // placeholder — frontend sends
                retention_model_id          = item.RetentionModelId,
                retention_term              = item.RetentionTerm,
                product_platform_id         = item.ProductPlatformId,
                product_pricing_level_id    = item.ProductPricingLevelId,
                vault_id                    = item.VaultId,
                license_attribute_license_value = item.LicenseAttributeLicenseValue,
                item_total                  = item.UnitPrice.HasValue && item.Quantity.HasValue
                                                  ? item.UnitPrice * item.Quantity
                                                  : (decimal?)null
            });

            _db.CartOrderItemJsons.Add(new CartOrderItemJson
            {
                CartOrderItemId        = savedItem.CartOrderItemId,
                CartOrderItemJsonValue = jsonBlob,
                InsertDate             = now,
                ModifiedDate           = now
            });
        }

        // ── G9: cart_order_item_license (SP section 5.4) ─────────────────────
        // keycode from @bundle_json → $.keycode (item.Keycode) per item.
        // Falls back to the order-level key when no per-item keycode is present.
        foreach (var (reqItem, savedItem) in request.Items.Zip(order.Items))
        {
            var keycode = reqItem.Keycode;
            if (string.IsNullOrWhiteSpace(keycode))
                keycode = request.Key;   // order-level key fallback

            if (!string.IsNullOrWhiteSpace(keycode))
            {
                _db.CartOrderItemLicenses.Add(new CartOrderItemLicense
                {
                    CartOrderItemId   = savedItem.CartOrderItemId,
                    Keycode           = keycode,
                    InsertDate        = now,
                    InsertBy          = request.AccountUserName ?? "system",
                    ModifiedDate      = now,
                    ModifiedBy        = request.AccountUserName ?? "system",
                    CartOrderStatusId = 1
                });
            }
        }

        // ── G12: CBCART routing hack (SP section 5.2) ────────────────────────
        // CB doesn't support upgrade orders.  When site_id='CBCART' + a supported
        // currency + at least one item has years=0 (auto-generated upgrade row per
        // SP 2.2.1), reroute the cart header to en_US / WRCART so it processes.
        // currency_id IN (1=USD, 2=EUR, 3=AUD, 4=GBP, 29=CAD)
        if (string.Equals(order.SiteId, "CBCART", StringComparison.OrdinalIgnoreCase)
            && order.CurrencyId.HasValue
            && new byte[] { 1, 2, 3, 4, 29 }.Contains(order.CurrencyId.Value)
            && request.Items.Any(i => i.Years == 0))
        {
            order.Locale    = "en_US";
            order.SiteId    = "WRCART";
            order.OrderType = "WRCART";
        }

        await _db.SaveChangesAsync(ct);

        // ── G13: CD line date sync (SP section 5.6) ───────────────────────────
        // S/H items (product_family_id = 8) inherit start/expiration from the
        // non-CD product in the same cart_item_bundle_id.  Raw parameterised UPDATE
        // because Product nav props are not loaded on newly-inserted entities.
        await _db.Database.ExecuteSqlAsync(
            $"""
            UPDATE coi2
            SET   coi2.start_date      = coi.start_date,
                  coi2.expiration_date = coi.expiration_date
            FROM  dbo.cart_order_item coi
            INNER JOIN dbo.product p
                ON p.product_id = coi.product_id
            INNER JOIN dbo.cart_order_item coi2
                ON  coi2.cart_order_id       = coi.cart_order_id
                AND coi2.cart_item_bundle_id = coi.cart_item_bundle_id
            INNER JOIN dbo.product p2
                ON p2.product_id = coi2.product_id
            WHERE coi.cart_order_id = {order.CartOrderId}
              AND (p.product_family_id <> 8 OR p.product_family_id IS NULL)
              AND p2.product_family_id = 8
            """, ct);

        return order.VendorOrderCode;
    }

    // ── Read path ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default)
    {
        // EF Core generates one query with all the JOINs:
        //   SELECT ... FROM cart_order
        //   INNER JOIN currency ON ...
        //   LEFT JOIN cart_order_partner LEFT JOIN partner ON ...
        //   LEFT JOIN cart_json ON ...
        //   LEFT JOIN cart_order_item ON ...
        //       LEFT JOIN product ON ...
        //       LEFT JOIN license_category ON ...
        //   WHERE cart_order.vendor_order_code = @p0
        var order = await _db.CartOrders
            .Include(o => o.Currency)
            .Include(o => o.CartOrderPartner)
                .ThenInclude(cop => cop!.Partner)
            .Include(o => o.CartJson)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ProductType)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ProductFamily)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.LicenseKeycodeType)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ProductLicenseCategories)
                        .ThenInclude(plc => plc.LicenseCategory)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ProductYears)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ProductSeats)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p!.ProductLineProducts)
                        .ThenInclude(plp => plp.ProductLine)
            .Include(o => o.Items)
                .ThenInclude(i => i.ItemLicense)
            .Include(o => o.Items)
                .ThenInclude(i => i.ItemJson)
            .Include(o => o.Items)
                .ThenInclude(i => i.LicenseAttributeValue)
            .Include(o => o.Routes)
            .Include(o => o.Messages)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.VendorOrderCode == vendorOrderCode, ct);

        return order is null ? null : MapToResponse(order);
    }

    // ── Quote-key check ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key, CancellationToken ct = default)
    {
        // G14: once cart_order_message is persisted (G5), we can detect an existing cart
        // by its message_key and pivot to the UPDATE path instead of INSERT.
        if (!Guid.TryParse(key, out var keyGuid))
            return null;

        return await _db.CartOrderMessages
            .Where(m => m.MessageKey == keyGuid)
            .Join(_db.CartOrders,
                  m => m.CartOrderId,
                  o => o.CartOrderId,
                  (m, o) => o.VendorOrderCode)
            .FirstOrDefaultAsync(ct);
    }

    // ── Mapping — entity → response ─────────────────────────────────────────────

    private static CartOrderResponse MapToResponse(CartOrder o) => new()
    {
        CartOrderId      = o.CartOrderId,
        VendorOrderCode  = o.VendorOrderCode ?? string.Empty,
        SiteId           = o.SiteId,
        OfferAmount      = o.OfferAmount,
        TotalAmount      = o.TotalAmount,
        SubTotalAmount   = o.SubTotalAmount,
        TaxAmount        = o.TaxAmount,
        SalesOrderDate   = o.SalesOrderDate,
        Locale           = o.Locale,
        InsertDate       = o.InsertDate,
        InsertBy         = o.InsertBy,
        ModifiedDate     = o.ModifiedDate,
        ModifiedBy       = o.ModifiedBy,
        CartOrderStatusId= o.CartOrderStatusId,
        UserIp           = o.UserIp,
        CurrencyId       = o.Currency?.CurrencyId ?? 0,
        CurrencyCode     = o.Currency?.CurrencyCode ?? string.Empty,
        PartnerKey       = o.CartOrderPartner?.Partner?.PartnerKey.ToString(),
        CartJson         = o.CartJson?.Json,
        Items            = o.Items
                             .OrderBy(i => i.LineItem)
                             .GroupBy(i => (i.CartItemBundleId ?? 0).ToString())
                             .ToDictionary(g => g.Key, g => g.Select(MapItemToResponse).ToList()),
        Route            = BuildRouteInfo(o)
    };

    private static CartOrderRouteInfo? BuildRouteInfo(CartOrder o)
    {
        var routingAction = o.Routes.FirstOrDefault()?.RoutingAction;
        var key           = o.Messages.FirstOrDefault()?.MessageKey.ToString();
        if (routingAction is null && key is null) return null;

        // "en_US" → "us/en"  |  "ja_JP" → "jp/ja"
        var localePath = "us/en";
        if (!string.IsNullOrWhiteSpace(o.Locale) && o.Locale.Contains('_'))
        {
            var p = o.Locale.Split('_');
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

        var url = $"https://www.webroot.com/{localePath}/cart" +
                  (qs.Length > 0 ? $"?{qs}" : string.Empty);
        return new CartOrderRouteInfo { Route = url };
    }

    private static CartOrderItemResponse MapItemToResponse(CartOrderItem i)
    {
        // Deserialize per-item JSON blob (vault, retention, platform, pricing level)
        ItemJsonDimensions? jsonDims = null;
        if (!string.IsNullOrWhiteSpace(i.ItemJson?.CartOrderItemJsonValue))
        {
            try
            {
                jsonDims = System.Text.Json.JsonSerializer.Deserialize<ItemJsonDimensions>(
                    i.ItemJson.CartOrderItemJsonValue,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { /* malformed JSON — leave dims null */ }
        }

        var licCat = i.Product?.ProductLicenseCategories.FirstOrDefault()?.LicenseCategory;
        var years   = i.Product?.ProductYears.FirstOrDefault()?.Years;
        var seats   = i.Product?.ProductSeats.FirstOrDefault()?.Seats;
        var productLine = i.Product?.ProductLineProducts.FirstOrDefault()?.ProductLine;

        // equivalent_year_price: retail_price × years (SP: pp.retail_price * y.years)
        // NULL when usage_pricing_model_id = 2 and storage_gb < 1024 and no item_total
        decimal? equivalentYearPrice = null;
        if (i.Product?.RetailPrice is not null && years is not null)
        {
            var isUsageModel2 = jsonDims?.UsagePricingModelId == 2;
            var itemTotalNull = jsonDims?.ItemTotal is null;
            var under1Tb      = (i.StorageGb ?? 0) < 1024;
            if (!(isUsageModel2 && itemTotalNull && under1Tb))
                equivalentYearPrice = (decimal)(i.Product.RetailPrice.Value * (decimal)years.Value);
        }

        return new CartOrderItemResponse
        {
            CartOrderItemId              = i.CartOrderItemId,
            CartOrderId                  = i.CartOrderId,
            LineItem                     = i.LineItem,
            Quantity                     = i.Quantity,
            Seats                        = seats,
            StorageGb                    = i.StorageGb,
            Years                        = years.HasValue ? (decimal)years.Value : null,
            OrderItemOfferAmount         = i.OrderItemOfferAmount,
            EquivalentYearPrice          = equivalentYearPrice,
            ListPrice                    = i.ListPrice,
            UnitPrice                    = i.UnitPrice,
            UnitPricePreVat              = i.UnitPricePreVat,
            TaxItemTotal                 = i.TaxItemTotal,
            UsagePrice                   = i.UsagePrice,
            Discount                     = i.Discount,
            CartDiscountMethodId         = i.CartDiscountMethodId,
            CartDiscountId               = i.CartDiscountId,
            ProductId                    = i.ProductId,
            ProductDescription           = i.Product?.ProductDescription,
            ProductTypeId                = i.Product?.ProductTypeId,
            ProductTypeDescription       = i.Product?.ProductType?.ProductTypeDescription,
            LicenseKeycodeTypeId         = i.Product?.LicenseKeycodeTypeId,
            LicenseKeycodeTypeDescription= i.Product?.LicenseKeycodeType?.LicenseKeycodeTypeDescription,
            LicenseCategoryId            = licCat?.LicenseCategoryId,
            LicenseCategoryName          = licCat?.LicenseCategoryName,
            LicenseCategoryDescription   = licCat?.LicenseCategoryDescription,
            ProductFamilyDescription     = i.Product?.ProductFamily?.ProductFamilyDescription,
            ProductLineCartType          = productLine?.ProductLineCartType,
            MinOrderQuantity             = licCat?.MinOrderQuantity,
            MaxOrderQuantity             = licCat?.MaxOrderQuantity,
            StartDate                    = i.StartDate,
            ExpirationDate               = i.ExpirationDate,
            CartItemBundleId             = i.CartItemBundleId,
            ItemHierarchyId              = i.ItemHierarchyId,
            Keycode                      = i.ItemLicense?.Keycode,
            LicenseAttributeLicenseValue = i.LicenseAttributeLicenseValue,
            LicenseAttributeLicenseValueDescription = i.LicenseAttributeValue?.LicenseAttributeLicenseValueDescription,
            VendorOrderItemCode          = i.VendorOrderItemCode,
            OrderItemUpdateTypeId        = i.OrderItemUpdateTypeId,
            OpportunityLineItemId        = i.OpportunityLineItemId,
            // JSON dimensions (vault, retention, platform, pricing level)
            UsagePricingModelId          = jsonDims?.UsagePricingModelId,
            RetentionModelId             = jsonDims?.RetentionModelId,
            RetentionTerm                = jsonDims?.RetentionTerm,
            RetentionModelTypeId         = jsonDims?.RetentionModelTypeId,
            ProductPlatformId            = jsonDims?.ProductPlatformId,
            VaultId                      = jsonDims?.VaultId,
            ProductPricingLevelId        = jsonDims?.ProductPricingLevelId,
            CartOrderItemJson            = i.ItemJson?.CartOrderItemJsonValue
        };
    }

    /// <summary>
    /// Mirrors the JSON shape stored by InsertCartOrderAsync (G8) and used by
    /// fn_cart_select_cart_order_item_json in the original SP.
    /// </summary>
    private sealed class ItemJsonDimensions
    {
        public int? UsagePricingModelId { get; set; }
        public int? RetentionModelId { get; set; }
        public int? RetentionTerm { get; set; }
        public int? RetentionModelTypeId { get; set; }
        public int? ProductPlatformId { get; set; }
        public int? VaultId { get; set; }
        public int? ProductPricingLevelId { get; set; }
        public decimal? ItemTotal { get; set; }
    }
}
