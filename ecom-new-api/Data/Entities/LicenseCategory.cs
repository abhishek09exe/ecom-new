using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_category")]
public sealed class LicenseCategory
{
    [Key]
    [Column("license_category_id")]
    public byte LicenseCategoryId { get; set; }

    [Column("license_category_name")]
    [MaxLength(50)]
    public string LicenseCategoryName { get; set; } = default!;

    [Column("license_category_description")]
    [MaxLength(255)]
    public string? LicenseCategoryDescription { get; set; }

    [Column("min_order_quantity")]
    public int? MinOrderQuantity { get; set; }

    [Column("max_order_quantity")]
    public int? MaxOrderQuantity { get; set; }

    [Column("base_capability_id")]
    public int? BaseCapabilityId { get; set; }
}
