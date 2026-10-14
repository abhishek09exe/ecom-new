using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_discount")]
public sealed class CartDiscount
{
    [Key]
    [Column("cart_discount_id")]
    public int CartDiscountId { get; set; }

    [Column("cart_discount_type_id")]
    public byte CartDiscountTypeId { get; set; }

    [Column("cart_discount_status_id")]
    public byte CartDiscountStatusId { get; set; }
}
