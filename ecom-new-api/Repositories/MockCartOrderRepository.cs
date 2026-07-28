using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Repositories;

/// <summary>
/// Mock repository — returns hard-coded/in-memory data so the service and controller
/// layers can be developed and tested without a real database connection.
///
/// REPLACE THIS CLASS with a real implementation (EF Core + SqlClient) once DB access
/// is available. Register the real implementation in Program.cs in place of this one.
///
/// Each method documents the stored procedure(s) it must call and the exact behavior
/// it must reproduce from the PHP layer.
/// </summary>
public sealed class MockCartOrderRepository : ICartOrderRepository
{
    // ── In-memory store (development only) ─────────────────────────────────────
    private static int _nextId = 1000;
    private static readonly Dictionary<string, CartOrderResponse> _store = new();

    // ── Write path ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// TODO: REPLACE WITH ACTUAL
    /// Real implementation must:
    ///   1. Begin a SQL transaction.
    ///   2. Call usp_cart_insert_cart_order(@site_id, @locale, @user_ip, @cart_extension_json,
    ///         @response_code OUTPUT, @message OUTPUT).
    ///      The @cart_extension_json blob contains: currency_code, vendor_order_code, partner_key,
    ///      account_user_name, routing_action, sales_order_date, message_campaign_id,
    ///      message_campaign_platform, key (message_key), cart_discount_id.
    ///   3. If response_code != 0 → throw / return error.
    ///   4. For each item in request.Items:
    ///        Build @item_json from CartOrderItemRequest fields.
    ///        Build @bundle_json (keycode, license_attribute_license_value,
    ///            license_keycode_type_id, order_item_update_type_id,
    ///            message_key, cart_discount_id, product_pricing_level_id).
    ///        Call usp_cart_insert_cart_order_item(@vendor_order_code, @item_json, @bundle_json,
    ///            @response_code OUTPUT, @message OUTPUT).
    ///   5. Commit transaction.
    ///   6. Return the vendor_order_code (output param from step 2 or supplied by caller).
    public Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _nextId);

        // TODO: REPLACE WITH ACTUAL — call usp_next_id(Type=3) + site prefix lookup table
        var vendorOrderCode = string.IsNullOrWhiteSpace(request.VendorOrderCode)
            ? $"MOCK-{id}"
            : request.VendorOrderCode;

        var now = DateTime.UtcNow;

        var response = new CartOrderResponse
        {
            CartOrderId = id,
            VendorOrderCode = vendorOrderCode,
            SiteId = request.SiteId,
            Locale = request.Locale,
            CurrencyCode = request.CurrencyCode ?? "USD", // TODO: REPLACE WITH ACTUAL — partner config fallback
            CurrencyId = 1,                               // TODO: REPLACE WITH ACTUAL — resolve from currency table
            CartOrderStatusId = 1,                        // TODO: REPLACE WITH ACTUAL — initial status from cart_order_status
            SalesOrderDate = request.SalesOrderDate,
            InsertDate = now,
            InsertBy = "system",                          // TODO: REPLACE WITH ACTUAL — SUSER_SNAME() from SP
            UserIp = request.UserIp,
            PartnerKey = request.PartnerKey,
            CartJson = request.VendorOrderCode is not null ? null : null, // populated after real insert
            Items = request.Items.Select((item, i) => new CartOrderItemResponse
            {
                CartOrderItemId = id * 100 + i + 1,
                CartOrderId = id,
                LineItem = i + 1,
                ProductId = item.ProductId,
                LicenseCategoryName = item.LicenseCategoryName,
                Quantity = item.Quantity ?? 1,
                Seats = item.LicenseSeats,                // SP stores as both quantity and total_license_seats
                StorageGb = item.StorageGb,
                Years = item.Years,
                ItemHierarchyId = item.ItemHierarchyId,
                CartItemBundleId = item.CartItemBundleId,
                StartDate = item.StartDate,
                ExpirationDate = item.ExpirationDate,
                VendorOrderItemCode = item.VendorOrderItemCode,
                Discount = item.Discount,
                CartDiscountMethodId = item.CartDiscountMethodId,
                CartDiscountId = item.CartDiscountId,
                LicenseAttributeLicenseValue = item.LicenseAttributeLicenseValue,
                UsagePricingModelId = item.UsagePricingModelId,
                RetentionModelId = item.RetentionModelId,
                RetentionTerm = item.RetentionTerm,
                ProductPlatformId = item.ProductPlatformId,
                VaultId = item.VaultId,
                OpportunityLineItemId = item.OpportunityLineItemId,
                // Pricing fields below are NULL in mock — populated only from the real re-read SP
                // TODO: REPLACE WITH ACTUAL — these come from usp_cart_select_cart_order_item
                UnitPrice = null,
                ListPrice = null,
                UnitPricePreVat = null,
                TaxItemTotal = null,
                UsagePrice = null,
                OrderItemOfferAmount = null,
                EquivalentYearPrice = null,
                ProductPricingLevelId = item.ProductPricingLevelId
            }).GroupBy(item => (item.CartItemBundleId ?? 0).ToString())
              .ToDictionary(g => g.Key, g => g.ToList())
        };

        _store[vendorOrderCode] = response;
        return Task.FromResult(vendorOrderCode);
    }

    /// <inheritdoc/>
    /// TODO: REPLACE WITH ACTUAL
    /// Real implementation must call:
    ///   usp_cart_select_cart_order(@vendor_order_code) — returns header row
    ///     (cart_order JOIN cart_order_partner JOIN partner JOIN currency JOIN cart_json)
    ///   usp_cart_select_cart_order_item(@vendor_order_code) — returns all item rows with
    ///     full computed pricing, vault JSON, retention model, product platform, etc.
    public Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default)
    {
        _store.TryGetValue(vendorOrderCode, out var order);
        return Task.FromResult(order);
    }

    // ── Quote-key check ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// TODO: REPLACE WITH ACTUAL
    /// Real implementation must:
    ///   Query cart_order_message JOIN cart_order WHERE message_key = @key
    ///   AND cart_order_status = 'pending' (quote state).
    ///   Return vendor_order_code if found, else null.
    public Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key, CancellationToken ct = default)
    {
        // Mock: no existing quote carts — always returns null (insert path)
        // TODO: REPLACE WITH ACTUAL — see description above
        return Task.FromResult<string?>(null);
    }
}

