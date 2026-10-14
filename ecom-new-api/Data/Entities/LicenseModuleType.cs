using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_module_type")]
public sealed class LicenseModuleType
{
    [Key, Column("license_module_type_id")]
    public byte LicenseModuleTypeId { get; set; }
    [Column("license_module_type_name"), MaxLength(50)]
    public string LicenseModuleTypeName { get; set; } = string.Empty;
}