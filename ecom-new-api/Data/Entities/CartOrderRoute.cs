namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps to [cart_order_route].
/// SP usp_cart_insert_cart_order section 2.4:
///   INSERT INTO cart_order_route (cart_order_id, routing_action, insert_date)
/// </summary>
public sealed class CartOrderRoute
{
    public int CartOrderRouteId { get; set; }
    public int CartOrderId { get; set; }
    public string RoutingAction { get; set; } = string.Empty;
    public DateTime InsertDate { get; set; }

    public CartOrder? CartOrder { get; set; }
}
