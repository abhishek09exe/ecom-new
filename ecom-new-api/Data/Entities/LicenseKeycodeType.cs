using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_keycode_type")]
public sealed class LicenseKeycodeType
{
    [Key]
    [Column("license_keycode_type_id")]
    public int LicenseKeycodeTypeId { get; set; }

    [Column("license_keycode_type_description")]
    [MaxLength(255)]
    public string? LicenseKeycodeTypeDescription { get; set; }
}
