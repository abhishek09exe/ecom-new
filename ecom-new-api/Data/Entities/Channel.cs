using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("channel")]
public sealed class Channel
{
    [Key]
    [Column("channel_id")]
    public int ChannelId { get; set; }

    [Column("channel_name")]
    [MaxLength(100)]
    public string ChannelName { get; set; } = default!;
}
