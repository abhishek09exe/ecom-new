using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("cart_order_message")]
public sealed class CartOrderMessage
{
    [Key]
    [Column("cart_order_message_id")]
    public int CartOrderMessageId { get; set; }

    [Column("cart_order_id")]
    public int CartOrderId { get; set; }

    [Column("status_id")]
    public byte StatusId { get; set; }

    [Column("message_key")]
    public Guid MessageKey { get; set; }

    [Column("message_campaign_id")]
    public int? MessageCampaignId { get; set; }

    [Column("message_campaign_platform")]
    [MaxLength(50)]
    public string? MessageCampaignPlatform { get; set; }

    [Column("cart_discount_id")]
    public int? CartDiscountId { get; set; }

    [Column("license_id")]
    public int? LicenseId { get; set; }
}
