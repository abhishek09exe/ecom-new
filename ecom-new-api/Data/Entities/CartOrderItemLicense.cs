using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_order_item_license")]
public sealed class CartOrderItemLicense
{
    [Key]
    [Column("cart_order_item_license_id")]
    public int CartOrderItemLicenseId { get; set; }

    [Column("cart_order_item_id")]
    public int CartOrderItemId { get; set; }

    [Column("keycode")]
    [MaxLength(100)]
    public string? Keycode { get; set; }
}
