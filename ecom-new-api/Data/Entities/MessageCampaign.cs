using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("message_campaign")]
public sealed class MessageCampaign
{
    [Key]
    [Column("message_campaign_id")]
    public int MessageCampaignId { get; set; }

    [Column("message_campaign_class_id")]
    public byte? MessageCampaignClassId { get; set; }

    [Column("message_campaign_enabled")]
    public byte MessageCampaignEnabled { get; set; }

    [Column("message_campaign_start_date")]
    public DateTime MessageCampaignStartDate { get; set; }

    [Column("message_campaign_end_date")]
    public DateTime? MessageCampaignEndDate { get; set; }
}
