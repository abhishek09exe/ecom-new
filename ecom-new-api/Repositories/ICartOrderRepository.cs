using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Repositories;

/// <summary>
/// Data-access contract for cart orders.
/// Each method maps directly to one stored procedure.
///
/// Note on identifiers: both usp_cart_select_cart_order and usp_cart_select_cart_order_item
/// take @vendor_order_code (not cart_order_id) as their lookup key. All read/select
/// methods here use vendorOrderCode accordingly.
/// </summary>
public interface ICartOrderRepository
{
    // ── Write path (per stored procedure) ──────────────────────────────────────

    /// <summary>
    /// EF Core equivalent of usp_cart_insert_cart_order.
    /// Inserts cart_order header and optional partner/route/message/cart_json rows.
    /// Returns the generated vendor_order_code.
    /// </summary>
    Task<string> InsertCartOrderHeaderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default);

    /// <summary>
    /// EF Core equivalent of usp_cart_insert_cart_order_item.
    /// Inserts a single cart_order_item row for the given order.
    /// </summary>
    Task InsertCartOrderItemAsync(
        int cartOrderId, string vendorOrderCode, CartOrderItemRequest item, int lineItem, CancellationToken ct = default);

    // ── Read path (per stored procedure) ───────────────────────────────────────

    /// <summary>
    /// EF Core equivalent of usp_cart_select_cart_order.
    /// Returns the cart header joined with partner, currency, and cart_json.
    /// </summary>
    Task<CartOrderResponse?> SelectCartOrderHeaderAsync(
        string vendorOrderCode, CancellationToken ct = default);

    /// <summary>
    /// EF Core equivalent of usp_cart_select_cart_order_item.
    /// Returns all item rows with product info, pricing, and JSON-derived fields.
    /// </summary>
    Task<List<CartOrderItemResponse>> SelectCartOrderItemsAsync(
        string vendorOrderCode, CancellationToken ct = default);

    // ── Composite operations (used by the service layer) ───────────────────────

    /// <summary>
    /// Calls InsertCartOrderHeaderAsync then InsertCartOrderItemAsync for each item.
    /// Returns the generated vendor_order_code.
    /// </summary>
    Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Calls SelectCartOrderHeaderAsync + SelectCartOrderItemsAsync and combines them
    /// into a fully hydrated CartOrderResponse (the shape the API returns).
    /// </summary>
    Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default);

    // ── Quote-key check (pivot create → update) ─────────────────────────────────

    /// <summary>
    /// Checks whether the given key resolves to an existing pending (quote) cart.
    /// If it does, the service must call UpdateCartOrderAsync instead of inserting.
    ///
    /// Maps to: usp_cart_select_message_key (partial — checks cart_order_message
    /// joined to cart_order where message_key = @key and status = pending/quote).
    ///
    /// Returns the vendor_order_code of the existing cart if found, else null.
    ///
    /// TODO: REPLACE WITH ACTUAL — query message_key / cart_order tables.
    /// </summary>
    Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key, CancellationToken ct = default);

    // ── Read path (GET endpoints) ───────────────────────────────────────────────

    /// <summary>
    /// Fetches license + available products for a keycode.
    ///
    /// Maps to:
    ///   usp_cart_select_message_key(@key) — resolves keycode to license record
    ///   usp_license_select_license_by_id(@license_id) — license details
    ///   usp_cart_select_license_profile(@license_id) — trial/full product profile
    ///   usp_cart_select_license_billing_model(@license_id) — billing model tooltip data
    ///
    /// TODO: REPLACE WITH ACTUAL — implement when DB is available.
    /// </summary>
    Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
        string keycode, CancellationToken ct = default);

    /// <summary>
    /// Returns renewal product options for a license.
    ///
    /// Maps to: usp_partner_cart_select_order_page_details (renew context)
    ///
    /// TODO: REPLACE WITH ACTUAL — implement when DB is available.
    /// </summary>
    Task<ConfigureResponse?> SelectConfigureAsync(
        string keycode, CancellationToken ct = default);

    /// <summary>
    /// Returns upgrade product options for a license.
    ///
    /// Maps to: usp_product_select_license_category_upgrade
    ///
    /// TODO: REPLACE WITH ACTUAL — implement when DB is available.
    /// </summary>
    Task<UpgradeResponse?> SelectUpgradeAsync(
        string keycode, CancellationToken ct = default);
}

