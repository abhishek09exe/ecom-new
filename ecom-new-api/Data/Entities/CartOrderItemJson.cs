using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_order_item_json")]
public sealed class CartOrderItemJson
{
    [Key]
    [Column("cart_order_item_json_id")]
    public int CartOrderItemJsonId { get; set; }

    [Column("cart_order_item_id")]
    public int CartOrderItemId { get; set; }

    [Column("cart_order_item_json")]
    public string Json { get; set; } = default!;

    [Column("insert_date")]
    public DateTime InsertDate { get; set; }

    [Column("modified_date")]
    public DateTime ModifiedDate { get; set; }
}
