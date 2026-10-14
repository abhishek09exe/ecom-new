using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("message_campaign_platform")]
public sealed class MessageCampaignPlatform
{
    [Key]
    [Column("message_campaign_platform_id")]
    public int MessageCampaignPlatformId { get; set; }

    [Column("message_campaign_id")]
    public int MessageCampaignId { get; set; }

    [Column("message_platform_id")]
    public byte MessagePlatformId { get; set; }
}
