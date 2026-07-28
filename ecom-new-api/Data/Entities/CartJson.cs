namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to the cart_json table.
/// Stores the extension JSON blob for a cart order (one-to-one with cart_order).
/// </summary>
public sealed class CartJson
{
    public int CartJsonId { get; set; }
    public string Json { get; set; } = default!;     // NOT NULL in QA
    public int? CartOrderId { get; set; }
    public int? CartOrderInProcessId { get; set; }

    // Navigation
    public CartOrder? CartOrder { get; set; }
}
