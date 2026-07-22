using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents the mapping between license categories and product lines.
/// Maps to [ecommerce_VH14].[dbo].[license_category_product_line]
/// 
/// Section 1.9 & 1.9.1: Lookup product_line_id by license_category and locale
/// Used for determining upgrade product line context
/// </summary>
[Table("license_category_product_line")]
public class LicenseCategoryProductLineEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("license_category_product_line_id")]
    public int LicenseCategoryProductLineId { get; set; }

    [Column("license_category_id")]
    public int? LicenseCategoryId { get; set; }

    [Column("product_line_id")]
    public int? ProductLineId { get; set; }

    [Column("language_code")]
    [MaxLength(10)]
    public string? LanguageCode { get; set; }

    [Column("location_code")]
    [MaxLength(10)]
    public string? LocationCode { get; set; }

    [Column("created_date")]
    public DateTime? CreatedDate { get; set; }

    [Column("modified_date")]
    public DateTime? ModifiedDate { get; set; }

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Navigation properties
    // ──────────────────────────────────────────────────────────────────────────────────────

    [ForeignKey(nameof(LicenseCategoryId))]
    public virtual LicenseCategoryEntity? LicenseCategory { get; set; }
}
