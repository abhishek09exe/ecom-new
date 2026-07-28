using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_order_partner")]
public class CartOrderPartner
{
    [Key]
    [Column("cart_order_partner_id")]
    public int CartOrderPartnerId { get; set; }

    [Column("cart_order_id")]
    public int CartOrderId { get; set; }

    [Column("partner_id")]
    public int PartnerId { get; set; }

    [Column("partner_account_id")]
    public int? PartnerAccountId { get; set; }

    public CartOrder CartOrder { get; set; } = null!;

    public Partner Partner { get; set; } = null!;
}