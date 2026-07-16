using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Repositories;

/// <summary>
/// Data-access contract for cart orders.
/// Each method maps directly to one or more stored procedures.
/// Swap MockCartOrderRepository for a real EF Core / SqlClient implementation
/// once DB access is available.
///
/// Note on identifiers: both usp_cart_select_cart_order and usp_cart_select_cart_order_item
/// take @vendor_order_code (not cart_order_id) as their lookup key. All read/select
/// methods here use vendorOrderCode accordingly.
/// </summary>
public interface ICartOrderRepository
{
    // ── Write path ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts the cart header and all related rows, then returns the generated vendor_order_code.
    ///
    /// Maps to:
    ///   usp_cart_insert_cart_order(@site_id, @locale, @user_ip, @cart_extension_json,
    ///       @response_code OUTPUT, @message OUTPUT)
    ///   usp_cart_insert_cart_order_item(@vendor_order_code, @item_json, @bundle_json,
    ///       @response_code OUTPUT, @message OUTPUT)  — called once per item in request.Items
    ///
    /// Returns the generated vendor_order_code (used as the key for all subsequent reads).
    /// </summary>
    Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Re-reads the full cart aggregate (header + items) after insert/update.
    ///
    /// Maps to:
    ///   usp_cart_select_cart_order(@vendor_order_code) — header row
    ///   usp_cart_select_cart_order_item(@vendor_order_code) — item rows
    ///
    /// This is what the API returns — NOT the raw insert output.
    /// The frontend depends on computed fields (pricing, equivalent_year_price, vault, etc.)
    /// that are only present in this re-read.
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

