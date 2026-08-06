using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_capability")]
public sealed class LicenseCapability
{
    [Key]
    [Column("license_capability_id")]
    public int LicenseCapabilityId { get; set; }

    [Column("license_id")]
    public int LicenseId { get; set; }

    [Column("capability_id")]
    public int CapabilityId { get; set; }

    [Column("capability_type_id")]
    public int CapabilityTypeId { get; set; }
}
