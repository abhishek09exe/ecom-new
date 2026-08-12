using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_attribute_license_value")]
public sealed class LicenseAttributeLicenseValue
{
    // PK is the value itself (same name as the table)
    [Key]
    [Column("license_attribute_license_value")]
    public int Value { get; set; }

    [Column("license_attribute_license_value_description")]
    [MaxLength(255)]
    public string? Description { get; set; }
}
