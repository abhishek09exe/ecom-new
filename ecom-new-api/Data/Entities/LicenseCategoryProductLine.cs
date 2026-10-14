using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_category_product_line")]
public sealed class LicenseCategoryProductLine
{
    [Key]
    [Column("license_category_product_line_id")]
    public int LicenseCategoryProductLineId { get; set; }

    [Column("license_category_id")]
    public byte LicenseCategoryId { get; set; }

    [Column("product_line_id")]
    public int ProductLineId { get; set; }

    [Column("cart_type_id")]
    public byte CartTypeId { get; set; }

    [Column("language_code")]
    [MaxLength(2)]
    public string LanguageCode { get; set; } = default!;

    [Column("location_code")]
    [MaxLength(3)]
    public string LocationCode { get; set; } = default!;

    public LicenseCategory? LicenseCategory { get; set; }
}