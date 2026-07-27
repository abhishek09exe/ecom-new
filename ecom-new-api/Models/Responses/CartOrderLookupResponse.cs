namespace ecom_new_api.Models.Responses;

/// <summary>
/// SQL Section 1.1 lookup result from usp_cart_insert_cart_order_item.
///
/// Maps to:
///   SELECT cart_order_id, currency_id
///   FROM dbo.cart_order
///   WHERE vendor_order_code = @vendor_order_code
/// </summary>
public sealed class CartOrderLookupResponse
{
    /// <summary>cart_order.cart_order_id</summary>
    public int CartOrderId { get; init; }

    /// <summary>cart_order.currency_id</summary>
    public int CurrencyId { get; init; }
}
