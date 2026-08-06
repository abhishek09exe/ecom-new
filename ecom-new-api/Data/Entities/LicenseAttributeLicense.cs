using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_attribute_license")]
public sealed class LicenseAttributeLicense
{
    [Key]
    [Column("license_attribute_license_id")]
    public int LicenseAttributeLicenseId { get; set; }

    [Column("license_attribute_id")]
    public int LicenseAttributeId { get; set; }

    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("license_attribute_license_value")]
    public int? LicenseAttributeLicenseValue { get; set; }

    [Column("modified_date")]
    public DateTime ModifiedDate { get; set; }
}
