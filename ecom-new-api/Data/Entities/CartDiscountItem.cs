using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_discount_item")]
public sealed class CartDiscountItem
{
    [Key]
    [Column("cart_discount_item_id")]
    public int CartDiscountItemId { get; set; }

    [Column("cart_discount_id")]
    public int CartDiscountId { get; set; }

    [Column("cart_discount_method_id")]
    public byte CartDiscountMethodId { get; set; }

    [Column("discount")]
    public double Discount { get; set; }

    [Column("low_range")]
    public double? LowRange { get; set; }

    [Column("high_range")]
    public double? HighRange { get; set; }

    [Column("product_type_id")]
    public int ProductTypeId { get; set; }

    [Column("product_line_id")]
    public int ProductLineId { get; set; }

    [Column("license_category_id")]
    public byte? LicenseCategoryId { get; set; }

    [Column("license_category_name")]
    [MaxLength(10)]
    public string? LicenseCategoryName { get; set; }

    [Column("license_seats")]
    public int? LicenseSeats { get; set; }

    [Column("storage_gb")]
    public int? StorageGb { get; set; }

    [Column("years")]
    public double? Years { get; set; }
}
