using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Links cart order items to license keycodes
/// Maps to [ecommerce_VH14].[dbo].[cart_order_item_license]
/// </summary>
[Table("cart_order_item_license")]
public class CartOrderItemLicenseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("cart_order_item_license_id")]
    public int CartOrderItemLicenseId { get; set; }

    [Required]
    [Column("cart_order_item_id")]
    [ForeignKey("CartOrderItem")]
    public int CartOrderItemId { get; set; }

    [MaxLength(40)]
    [Column("keycode")]
    public string? Keycode { get; set; }

    [Column("insert_date")]
    public DateTime? InsertDate { get; set; }

    [MaxLength(256)]
    [Column("insert_by")]
    public string? InsertBy { get; set; }

    [Column("modified_date")]
    public DateTime? ModifiedDate { get; set; }

    [MaxLength(256)]
    [Column("modified_by")]
    public string? ModifiedBy { get; set; }

    // Navigation properties
    public virtual CartOrderItemEntity CartOrderItem { get; set; } = null!;
}
