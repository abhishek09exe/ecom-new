using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_type")]
public sealed class LicenseType
{
    [Key]
    [Column("license_type_id")]
    public int LicenseTypeId { get; set; }

    [Column("license_type_description")]
    [MaxLength(20)]
    public string LicenseTypeDescription { get; set; } = default!;
}
