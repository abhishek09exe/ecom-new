namespace ecom_new_api.Models.Responses;

/// <summary>
/// SQL Section 1.2 lookup result from usp_cart_insert_cart_order_item.
///
/// Maps to:
///   SELECT co.locale, co.site_id, cp.partner_id
///   FROM dbo.cart_order co
///   LEFT JOIN dbo.cart_order_partner cp ON cp.cart_order_id = co.cart_order_id
///   WHERE co.cart_order_id = @cart_order_id
/// </summary>
public sealed class CartOrderContextLookupResponse
{
    /// <summary>cart_order.locale</summary>
    public string? Locale { get; set; }

    /// <summary>cart_order.site_id</summary>
    public string? SiteId { get; set; }

    /// <summary>cart_order_partner.partner_id (nullable for LEFT JOIN)</summary>
    public int? PartnerId { get; set; }
}
