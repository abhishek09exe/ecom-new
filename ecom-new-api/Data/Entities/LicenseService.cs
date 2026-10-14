using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_service")]
public sealed class LicenseService
{
    [Key, Column("license_service_id")]
    public int LicenseServiceId { get; set; }
    [Column("license_service_type_id")]
    public byte LicenseServiceTypeId { get; set; }
    [Column("license_service_status_id")]
    public byte LicenseServiceStatusId { get; set; }
    [Column("insert_date")]
    public DateTime InsertDate { get; set; }
    [Column("process_date")]
    public DateTime? ProcessDate { get; set; }
    [Column("update_attempts")]
    public byte UpdateAttempts { get; set; }
}