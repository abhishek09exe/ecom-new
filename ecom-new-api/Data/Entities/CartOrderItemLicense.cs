namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [cart_order_item_license].
/// SP usp_cart_insert_cart_order_item section 5.4:
///   IF @keycode IS NOT NULL
///     INSERT INTO cart_order_item_license (cart_order_item_id, keycode, insert_date, ...)
/// Links each inserted cart item to its renewal/upgrade keycode.
/// </summary>
public sealed class CartOrderItemLicense
{
    public int CartOrderItemLicenseId { get; set; }
    public int CartOrderItemId { get; set; }
    public string Keycode { get; set; } = string.Empty;
    public DateTime InsertDate { get; set; }
    public string InsertBy { get; set; } = string.Empty;
    public DateTime ModifiedDate { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public byte CartOrderStatusId { get; set; }

    public CartOrderItem? CartOrderItem { get; set; }
}
