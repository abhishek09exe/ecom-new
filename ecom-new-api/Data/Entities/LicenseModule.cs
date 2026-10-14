using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_module")]
public sealed class LicenseModule
{
    [Key, Column("license_module_id")]
    public byte LicenseModuleId { get; set; }
    [Column("license_module_code"), MaxLength(50)]
    public string LicenseModuleCode { get; set; } = string.Empty;
    [Column("license_module_class_id")]
    public byte LicenseModuleClassId { get; set; }
}