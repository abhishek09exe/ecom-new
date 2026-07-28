namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [cart_order_item_json_log].
/// SP usp_cart_insert_cart_order_item section 5.0:
///   INSERT INTO cart_order_item_json_log (cart_order_id, item_json, bundle_json)
///   VALUES (@cart_order_id, @item_json, @bundle_json)
/// Raw JSON audit log — one row per cart-create API call, stored before any item processing.
/// Schema: cart_order_item_json_log_id INT IDENTITY, cart_order_id INT,
///         item_json NVARCHAR(MAX), bundle_json NVARCHAR(MAX), insert_date DATETIME
/// </summary>
public sealed class CartOrderItemJsonLog
{
    public int CartOrderItemJsonLogId { get; set; }
    public int CartOrderId { get; set; }
    public string? ItemJson { get; set; }
    public string? BundleJson { get; set; }
    public DateTime InsertDate { get; set; }

    public CartOrder? CartOrder { get; set; }
}
