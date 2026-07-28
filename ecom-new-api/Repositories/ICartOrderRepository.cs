using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Repositories;

/// <summary>
/// Data-access contract for cart orders.
/// Implemented by <see cref="EfCartOrderRepository"/> using pure EF Core —
/// no stored procedures, EF generates all SQL.
/// </summary>
public interface ICartOrderRepository
{
    // ── Write path ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts the cart order header and all line items, then returns the generated
    /// vendor_order_code.
    ///
    /// EF Core generates:
    ///   INSERT INTO cart_order (...)        VALUES (...)
    ///   INSERT INTO cart_order_item (...)   VALUES (...)  — once per item
    ///   INSERT INTO cart_json (...)         VALUES (...)
    ///   INSERT INTO cart_order_partner (...) VALUES (...) — if partner_key supplied
    /// </summary>
    Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Reads the full cart aggregate (header + items + currency + partner) after insert/update.
    /// This is what the API returns to the frontend.
    ///
    /// EF Core generates:
    ///   SELECT ... FROM cart_order
    ///   INNER JOIN currency ON ...
    ///   LEFT JOIN cart_order_partner / partner ON ...
    ///   LEFT JOIN cart_json ON ...
    ///   LEFT JOIN cart_order_item ON ...
    ///       LEFT JOIN product ON ...
    ///       LEFT JOIN license_category ON ...
    ///   WHERE vendor_order_code = @p0
    /// </summary>
    Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default);

    // ── Quote-key check (pivot create → update) ─────────────────────────────────

    /// <summary>
    /// Checks whether the given key resolves to an existing pending (quote) cart.
    /// Returns the vendor_order_code of the existing cart if found, else null.
    /// TODO: implement once cart_order_message table is mapped.
    /// </summary>
    Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key, CancellationToken ct = default);
}
