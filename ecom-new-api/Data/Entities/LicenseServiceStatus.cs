using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_service_status")]
public sealed class LicenseServiceStatus
{
    [Key, Column("license_service_status_id")]
    public byte LicenseServiceStatusId { get; set; }
    [Column("license_service_status_name"), MaxLength(50)]
    public string LicenseServiceStatusName { get; set; } = string.Empty;
}