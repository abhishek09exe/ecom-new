using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_status")]
public sealed class LicenseStatus
{
    [Key]
    [Column("license_status_id")]
    public int LicenseStatusId { get; set; }

    [Column("license_status_description")]
    [MaxLength(20)]
    public string LicenseStatusDescription { get; set; } = default!;
}
