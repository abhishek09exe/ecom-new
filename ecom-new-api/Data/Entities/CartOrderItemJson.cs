namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [cart_order_item_json].
/// SP usp_cart_insert_cart_order_item section 5.3.3:
///   INSERT INTO cart_order_item_json (cart_order_item_id, cart_order_item_json)
/// Stores per-item JSON dimensions: vault, retention, platform, pricing level, etc.
/// </summary>
public sealed class CartOrderItemJson
{
    public int CartOrderItemJsonId { get; set; }
    public int CartOrderItemId { get; set; }

    /// <summary>JSON blob containing vault, retention, platform, pricing_level, etc.</summary>
    public string CartOrderItemJsonValue { get; set; } = string.Empty;

    public DateTime InsertDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public CartOrderItem? CartOrderItem { get; set; }
}
