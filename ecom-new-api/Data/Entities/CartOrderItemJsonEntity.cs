using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Extension JSON blob for cart order items
/// Maps to [ecommerce_VH14].[dbo].[cart_order_item_json]
/// </summary>
[Table("cart_order_item_json")]
public class CartOrderItemJsonEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("cart_order_item_json_id")]
    public int CartOrderItemJsonId { get; set; }

    [Required]
    [Column("cart_order_item_id")]
    [ForeignKey("CartOrderItem")]
    public int CartOrderItemId { get; set; }

    [Column("cart_order_item_json")]
    public string? CartOrderItemJson { get; set; }

    // Navigation properties
    public virtual CartOrderItemEntity CartOrderItem { get; set; } = null!;
}
