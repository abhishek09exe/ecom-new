using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("message_campaign_language_location")]
public sealed class MessageCampaignLanguageLocation
{
    [Key]
    [Column("message_campaign_language_location_id")]
    public int MessageCampaignLanguageLocationId { get; set; }

    [Column("message_campaign_id")]
    public int MessageCampaignId { get; set; }

    [Column("language_code")]
    [MaxLength(2)]
    public string LanguageCode { get; set; } = default!;

    [Column("location_code")]
    [MaxLength(3)]
    public string LocationCode { get; set; } = default!;
}
