using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents a license attribute value (billing model codes like 110, 111, 112, etc.).
/// Maps to [ecommerce_VH14].[dbo].[license_attribute_license_value]
/// 
/// Section 1.3.2: Loads license billing model attribute value
/// This translates the @license_attribute_license_value column which represents billing models
/// </summary>
[Table("license_attribute_license_value")]
public class LicenseAttributeEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("license_attribute_license_value_id")]
    public int LicenseAttributeLicenseValueId { get; set; }

    [Column("license_id")]
    public int? LicenseId { get; set; }

    [Column("license_attribute_license_value")]
    public int? LicenseAttributeLicenseValue { get; set; }

    [Column("license_attribute_id")]
    public int? LicenseAttributeId { get; set; }

    [Column("license_attribute_value")]
    [MaxLength(100)]
    public string? LicenseAttributeValue { get; set; }

    [Column("created_date")]
    public DateTime? CreatedDate { get; set; }

    [Column("modified_date")]
    public DateTime? ModifiedDate { get; set; }
}
