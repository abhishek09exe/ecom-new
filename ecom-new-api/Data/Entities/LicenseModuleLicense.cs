using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_module_license")]
public sealed class LicenseModuleLicense
{
    [Key, Column("license_module_license_id")]
    public int LicenseModuleLicenseId { get; set; }
    [Column("license_id")]
    public int LicenseId { get; set; }
    [Column("license_module_id")]
    public byte LicenseModuleId { get; set; }
    [Column("license_module_type_id")]
    public byte LicenseModuleTypeId { get; set; }
    [Column("start_date", TypeName = "datetime")]
    public DateTime? StartDate { get; set; }
    [Column("end_date", TypeName = "datetime")]
    public DateTime? EndDate { get; set; }
    [Column("license_status_id")]
    public int? LicenseStatusId { get; set; }
    [Column("last_modified_date", TypeName = "datetime")]
    public DateTime LastModifiedDate { get; set; }
    [Column("last_modified_by"), MaxLength(200)]
    public string LastModifiedBy { get; set; } = string.Empty;
    [Column("module_seats")]
    public int ModuleSeats { get; set; }
}