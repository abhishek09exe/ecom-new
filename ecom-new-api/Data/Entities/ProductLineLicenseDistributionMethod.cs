using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_line_license_distribution_method")]
public sealed class ProductLineLicenseDistributionMethod
{
    [Key, Column("product_line_license_distribution_method_id")]
    public int ProductLineLicenseDistributionMethodId { get; set; }
    [Column("product_line_id")]
    public int ProductLineId { get; set; }
    [Column("license_keycode_type_id")]
    public int LicenseKeycodeTypeId { get; set; }
    [Column("trial")]
    public byte Trial { get; set; }
    [Column("license_distribution_method_id")]
    public int LicenseDistributionMethodId { get; set; }
    [Column("license_category_id")]
    public int? LicenseCategoryId { get; set; }
}