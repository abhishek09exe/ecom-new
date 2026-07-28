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
                nextId = (int)await cmd.ExecuteScalarAsync(ct);
            }
            if (!wasOpen) await conn.CloseAsync();

            vendorOrderCode = $"{prefix}{nextId:D8}";
        }

        var now = DateTime.UtcNow;

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
                LineItem                     = index + 1,
                ProductId                    = item.ProductId,
                InvoiceItemInProcessId       = 0,
                VendorId                     = 1,
                CartOrderStatusId            = 1,
                Quantity                     = item.Quantity ?? 1,
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

        // ── Link partner if found ──────────────────────────────────────────────
        if (partner is not null)
        {
            order.CartOrderPartner = new CartOrderPartner
            {
                PartnerId = partner.PartnerId
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
        // When a keycode (bundle keycode) is provided, link every inserted item to it.
        var bundleKeycode = request.Items
            .Select(i => i.VendorOrderItemCode)
            .FirstOrDefault(k => !string.IsNullOrWhiteSpace(k));

        if (!string.IsNullOrWhiteSpace(bundleKeycode))
        {
            foreach (var savedItem in order.Items)
            {
                _db.CartOrderItemLicenses.Add(new CartOrderItemLicense
                {
                    CartOrderItemId  = savedItem.CartOrderItemId,
                    Keycode          = bundleKeycode,
                    InsertDate       = now,
                    InsertBy         = request.AccountUserName ?? "system",
                    ModifiedDate     = now,
                    ModifiedBy       = request.AccountUserName ?? "system",
                    CartOrderStatusId = 1
                });
            }
        }

        await _db.SaveChangesAsync(ct);

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
        VendorOrderCode  = o.VendorOrderCode,
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
        CurrencyCode     = o.Currency?.CurrencyCode,
        PartnerKey       = o.CartOrderPartner?.Partner?.PartnerKey.ToString(),
        CartJson         = o.CartJson?.Json,
        Items            = o.Items.OrderBy(i => i.LineItem).Select(MapItemToResponse).ToList()
    };

    private static CartOrderItemResponse MapItemToResponse(CartOrderItem i) => new()
    {
        CartOrderItemId              = i.CartOrderItemId,
        CartOrderId                  = i.CartOrderId,
        LineItem                     = i.LineItem,
        Quantity                     = i.Quantity,
        StorageGb                    = i.StorageGb,
        OrderItemOfferAmount         = i.OrderItemOfferAmount,
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
        StartDate                    = i.StartDate,
        ExpirationDate               = i.ExpirationDate,
        CartItemBundleId             = i.CartItemBundleId,
        ItemHierarchyId              = i.ItemHierarchyId,
        LicenseAttributeLicenseValue = i.LicenseAttributeLicenseValue,
        VendorOrderItemCode          = i.VendorOrderItemCode,
        OrderItemUpdateTypeId        = i.OrderItemUpdateTypeId,
        OpportunityLineItemId        = i.OpportunityLineItemId,
    };
}
