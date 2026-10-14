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

    [Column("capability_activation_days")]
    public int? CapabilityActivationDays { get; set; }
    [Column("capability_activation_date", TypeName = "datetime")]
    public DateTime? CapabilityActivationDate { get; set; }
    [Column("capability_expiration_date", TypeName = "datetime")]
    public DateTime? CapabilityExpirationDate { get; set; }
    [Column("insert_date", TypeName = "datetime")]
    public DateTime InsertDate { get; set; }
    [Column("insert_by"), MaxLength(50)]
    public string InsertBy { get; set; } = string.Empty;
    [Column("modified_date", TypeName = "datetime")]
    public DateTime ModifiedDate { get; set; }
    [Column("modified_by"), MaxLength(50)]
    public string ModifiedBy { get; set; } = string.Empty;
}
