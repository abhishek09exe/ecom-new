using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Maps license billing-model assignments for a license keycode.
/// Table: [license_attribute_license]
/// </summary>
[Table("license_attribute_license")]
public class LicenseAttributeLicenseEntity
{
    [Key]
    [Column("license_attribute_license_id")]
    public int LicenseAttributeLicenseId { get; set; }

    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("license_attribute_license_value")]
    public int? LicenseAttributeLicenseValue { get; set; }
}
