using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_order_route")]
public sealed class CartOrderRoute
{
    [Key]
    [Column("cart_order_route_id")]
    public int CartOrderRouteId { get; set; }

    [Column("cart_order_id")]
    public int CartOrderId { get; set; }

    [Column("routing_action")]
    [MaxLength(50)]
    public string RoutingAction { get; set; } = default!;

    [Column("insert_date")]
    public DateTime InsertDate { get; set; }
}
