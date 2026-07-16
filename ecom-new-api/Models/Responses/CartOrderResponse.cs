namespace ecom_new_api.Models.Responses;

/// <summary>
/// API response for POST /cart/cart-orders (201) and cart re-read operations.
///
/// IMPORTANT: This is NOT the raw insert output. After insert the service re-reads
/// the full cart aggregate via usp_cart_select_cart_order + usp_cart_select_cart_order_item
/// and returns that hydrated shape. The frontend depends on computed fields only present
/// in the re-read (pricing, currency, item bundles).
///
/// Column source: usp_cart_select_cart_order SELECT list
/// (cart_order JOIN cart_order_partner JOIN partner JOIN currency JOIN cart_json)
/// </summary>
public sealed class CartOrderResponse
{
    // ── From cart_order ────────────────────────────────────────────────────────

    /// <summary>cart_order.cart_order_id</summary>
    public int CartOrderId { get; init; }

    /// <summary>cart_order.vendor_order_code — the SP takes this as @vendor_order_code parameter.</summary>
    public string VendorOrderCode { get; init; } = default!;

    /// <summary>cart_order.site_id</summary>
    public string SiteId { get; init; } = default!;

    /// <summary>cart_order.offer_amount — total after any offer/discount.</summary>
    public decimal? OfferAmount { get; init; }

    /// <summary>cart_order.total_amount — grand total including tax.</summary>
    public decimal? TotalAmount { get; init; }

    /// <summary>cart_order.sub_total_amount — pre-tax subtotal.</summary>
    public decimal? SubTotalAmount { get; init; }

    /// <summary>cart_order.tax_amount</summary>
    public decimal? TaxAmount { get; init; }

    /// <summary>cart_order.sales_order_date</summary>
    public DateTime SalesOrderDate { get; init; }

    /// <summary>cart_order.locale (CHAR 5, e.g. "en-US")</summary>
    public string Locale { get; init; } = default!;

    /// <summary>cart_order.insert_date</summary>
    public DateTime InsertDate { get; init; }

    /// <summary>cart_order.insert_by</summary>
    public string? InsertBy { get; init; }

    /// <summary>cart_order.modified_date</summary>
    public DateTime? ModifiedDate { get; init; }

    /// <summary>cart_order.modified_by</summary>
    public string? ModifiedBy { get; init; }

    /// <summary>cart_order.cart_order_status_id</summary>
    public int CartOrderStatusId { get; init; }

    /// <summary>cart_order.user_ip — always server-set, never client-supplied.</summary>
    public string? UserIp { get; init; }

    // ── From currency (JOIN on cart_order.currency_id) ─────────────────────────

    /// <summary>currency.currency_id</summary>
    public int CurrencyId { get; init; }

    /// <summary>currency.currency_code (e.g. "USD")</summary>
    public string CurrencyCode { get; init; } = default!;

    // ── From partner (LEFT JOIN via cart_order_partner) ────────────────────────

    /// <summary>
    /// partner.partner_key — CONVERT(varchar(36), ...) in the SP.
    /// Null if this is not a partner order.
    /// </summary>
    public string? PartnerKey { get; init; }

    // ── From cart_json (LEFT JOIN) ─────────────────────────────────────────────

    /// <summary>cart_json.cart_json — the raw extension JSON stored at order creation time.</summary>
    public string? CartJson { get; init; }

    // ── Hydrated line items (from usp_cart_select_cart_order_item) ─────────────

    /// <summary>Line items populated from usp_cart_select_cart_order_item re-read.</summary>
    public List<CartOrderItemResponse> Items { get; init; } = [];
}

