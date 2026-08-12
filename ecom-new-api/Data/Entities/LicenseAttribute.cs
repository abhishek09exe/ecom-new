using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_attribute")]
public sealed class LicenseAttribute
{
    [Key]
    [Column("license_attribute_id")]
    public int LicenseAttributeId { get; set; }

    [Column("license_attribute_description")]
    [MaxLength(100)]
    public string LicenseAttributeDescription { get; set; } = default!;

    [Column("license_attribute_tag")]
    [MaxLength(20)]
    public string LicenseAttributeTag { get; set; } = default!;
}
