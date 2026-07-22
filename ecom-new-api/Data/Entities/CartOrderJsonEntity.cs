using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Extension JSON blob for cart orders
/// Maps to [ecommerce_VH14].[dbo].[cart_json]
/// </summary>
[Table("cart_json")]
public class CartOrderJsonEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("cart_json_id")]
    public int CartJsonId { get; set; }

    [Required]
    [Column("cart_order_id")]
    [ForeignKey("CartOrder")]
    public int CartOrderId { get; set; }

    [Column("json")]
    public string? Json { get; set; }

    // Navigation properties
    public virtual CartOrderEntity CartOrder { get; set; } = null!;
}
