using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents a license category (SOHO, SMB, ENT, OTSF, etc.)
/// Maps to [ecommerce_VH14].[dbo].[license_category]
/// </summary>
[Table("license_category")]
public class LicenseCategoryEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("license_category_id")]
    public int LicenseCategoryId { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("license_category_name")]
    public string LicenseCategoryName { get; set; } = null!;

    [MaxLength(255)]
    [Column("license_category_description")]
    public string? LicenseCategoryDescription { get; set; }
}
