using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_json")]
public sealed class CartJson
{
    [Key]
    [Column("cart_json_id")]
    public int CartJsonId { get; set; }

    [Column("cart_order_id")]
    public int CartOrderId { get; set; }

    [Column("cart_json")]
    public string Json { get; set; } = default!;
}
