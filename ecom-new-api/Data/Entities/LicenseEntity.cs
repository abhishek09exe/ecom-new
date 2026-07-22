using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents a license (keycode) record.
/// Maps to [ecommerce_VH14].[dbo].[license]
/// 
/// Section 1.3.1: Loads license profile data by keycode
/// Section 1.5: fn_license_select_license_profile() returns license data
/// </summary>
[Table("license")]
public class LicenseEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("license_id")]
    public int LicenseId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("keycode")]
    public string Keycode { get; set; } = default!;

    [Column("license_category_id")]
    public int? LicenseCategoryId { get; set; }

    [MaxLength(50)]
    [Column("license_status")]
    public string? LicenseStatus { get; set; }

    [Column("license_seats")]
    public int? LicenseSeats { get; set; }

    [Column("expiration_date")]
    public DateTime? ExpirationDate { get; set; }

    [Column("autorenew_cycle")]
    public decimal? AutorenewCycle { get; set; }

    [Column("retention_model_id")]
    public byte? RetentionModelId { get; set; }

    [Column("retention_term")]
    public byte? RetentionTerm { get; set; }

    [Column("usage_pricing_model_id")]
    public byte? UsagePricingModelId { get; set; }

    [Column("product_platform_id")]
    public byte? ProductPlatformId { get; set; }

    [Column("license_keycode_type_id")]
    public int? LicenseKeycodeTypeId { get; set; }

    [Column("license_distribution_method_id")]
    public int? LicenseDistributionMethodId { get; set; }

    [Column("storage_gb")]
    public int? StorageGb { get; set; }

    [Column("category_type_name")]
    [MaxLength(20)]
    public string? CategoryTypeName { get; set; }

    [Column("product_line_id")]
    public int? ProductLineId { get; set; }

    // ──────────────────────────────────────────────────────────────────────────────────────
    // Navigation properties
    // ──────────────────────────────────────────────────────────────────────────────────────

    [ForeignKey(nameof(LicenseCategoryId))]
    public virtual LicenseCategoryEntity? LicenseCategory { get; set; }
}
