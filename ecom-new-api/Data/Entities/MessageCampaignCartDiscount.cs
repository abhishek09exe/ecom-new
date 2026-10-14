using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("message_campaign_cart_discount")]
public sealed class MessageCampaignCartDiscount
{
    [Key]
    [Column("message_campaign_cart_discount_int")]
    public int MessageCampaignCartDiscountId { get; set; }

    [Column("message_campaign_id")]
    public int MessageCampaignId { get; set; }

    [Column("cart_discount_id")]
    public int CartDiscountId { get; set; }
}
