using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Junction table linking cart orders to partners
/// Maps to [ecommerce_VH14].[dbo].[cart_order_partner]
/// </summary>
[Table("cart_order_partner")]
public class CartOrderPartnerEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("cart_order_partner_id")]
    public int CartOrderPartnerId { get; set; }

    [Required]
    [Column("cart_order_id")]
    [ForeignKey("CartOrder")]
    public int CartOrderId { get; set; }

    [Required]
    [Column("partner_id")]
    [ForeignKey("Partner")]
    public int PartnerId { get; set; }

    [MaxLength(256)]
    [Column("account_user_name")]
    public string? AccountUserName { get; set; }

    // Navigation properties
    public virtual CartOrderEntity CartOrder { get; set; } = null!;
    public virtual PartnerEntity Partner { get; set; } = null!;
}
