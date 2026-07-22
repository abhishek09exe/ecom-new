using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents a product in the catalog
/// Maps to [ecommerce_VH14].[dbo].[product]
/// </summary>
[Table("product")]
public class ProductEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("product_description")]
    public string ProductDescription { get; set; } = null!;

    [Column("product_type_id")]
    public int? ProductTypeId { get; set; }

    [Column("license_keycode_type_id")]
    public int? LicenseKeycodeTypeId { get; set; }

    [Column("license_category_id")]
    public int? LicenseCategoryId { get; set; }

    [Column("product_family_id")]
    public int? ProductFamilyId { get; set; }

    [Column("product_line_id")]
    public int? ProductLineId { get; set; }

    [Column("product_lifecycle_id")]
    public int? ProductLifecycleId { get; set; }

    // Navigation properties
    /// <summary>License category for this product (loaded via .Include() for optimization)</summary>
    public virtual LicenseCategoryEntity? LicenseCategory { get; set; }
    public virtual ICollection<CartOrderItemEntity> CartOrderItems { get; set; } = [];
}
