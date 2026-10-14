using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_module_category")]
public sealed class LicenseModuleCategory
{
    [Key, Column("license_module_category_id")]
    public int LicenseModuleCategoryId { get; set; }
    [Column("license_module_id")]
    public byte LicenseModuleId { get; set; }
    [Column("license_category_id")]
    public byte LicenseCategoryId { get; set; }
}