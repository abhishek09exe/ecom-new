using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_service_json")]
public sealed class LicenseServiceJson
{
    [Key, Column("license_service_json_id")]
    public int LicenseServiceJsonId { get; set; }
    [Column("license_service_id")]
    public int LicenseServiceId { get; set; }
    [Column("license_service_json")]
    public string ServiceJson { get; set; } = string.Empty;
}