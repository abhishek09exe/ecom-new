using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("partner_configuration_partner")]
public sealed class PartnerConfigurationPartner
{
    [Key]
    [Column("partner_configuration_partner_id")]
    public int PartnerConfigurationPartnerId { get; set; }

    [Column("partner_id")]
    public int PartnerId { get; set; }

    [Column("partner_configuration_id")]
    public int PartnerConfigurationId { get; set; }

    [Column("configuration_value")]
    [MaxLength(100)]
    public string? ConfigurationValue { get; set; }
}
