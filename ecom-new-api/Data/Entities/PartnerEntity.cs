using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Represents a partner/vendor
/// Maps to [ecommerce_VH14].[dbo].[partner]
/// </summary>
[Table("partner")]
public class PartnerEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("partner_id")]
    public int PartnerId { get; set; }

    [Required]
    [Column("partner_key")]
    public Guid PartnerKey { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("partner_name")]
    public string PartnerName { get; set; } = null!;

    // Navigation properties
    public virtual ICollection<CartOrderPartnerEntity> CartOrderPartners { get; set; } = [];
}
