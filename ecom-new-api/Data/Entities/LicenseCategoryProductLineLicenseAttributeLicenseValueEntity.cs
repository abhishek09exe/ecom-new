using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Mapping between license_category_product_line rows and billing-model values.
/// Table: [license_category_product_line_license_attribute_license_value]
/// </summary>
[Table("license_category_product_line_license_attribute_license_value")]
public class LicenseCategoryProductLineLicenseAttributeLicenseValueEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("license_category_product_line_license_attribute_license_value_id")]
    public int LicenseCategoryProductLineLicenseAttributeLicenseValueId { get; set; }

    [Column("license_category_product_line_id")]
    public int LicenseCategoryProductLineId { get; set; }

    [Column("license_attribute_license_value")]
    public int LicenseAttributeLicenseValue { get; set; }
}
