using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("license_distribution_method_channel")]
public sealed class LicenseDistributionMethodChannel
{
    [Key]
    [Column("license_distribution_method_channel_id")]
    public int LicenseDistributionMethodChannelId { get; set; }

    [Column("channel_id")]
    public int ChannelId { get; set; }

    [Column("license_distribution_method_id")]
    public int LicenseDistributionMethodId { get; set; }
}
