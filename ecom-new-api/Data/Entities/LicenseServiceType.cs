using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_service_type")]
public sealed class LicenseServiceType
{
    [Key, Column("license_service_type_id")]
    public byte LicenseServiceTypeId { get; set; }
    [Column("license_service_type_name"), MaxLength(50)]
    public string LicenseServiceTypeName { get; set; } = string.Empty;
    [Column("process_type"), MaxLength(20)]
    public string? ProcessType { get; set; }
    [Column("license_service_type_description")]
    public string? LicenseServiceTypeDescription { get; set; }
}