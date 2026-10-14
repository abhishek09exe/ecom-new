using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_category_capability")]
public sealed class LicenseCategoryCapability
{
    [Key, Column("license_category_capability_id")]
    public int LicenseCategoryCapabilityId { get; set; }
    [Column("license_category_id")]
    public int LicenseCategoryId { get; set; }
    [Column("capability_id")]
    public int CapabilityId { get; set; }
}