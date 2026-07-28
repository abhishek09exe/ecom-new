using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("partner")]
public class Partner
{
    [Key]
    [Column("partner_id")]
    public int PartnerId { get; set; }

    [Column("partner_name")]
    public string PartnerName { get; set; } = string.Empty;

    [Column("partner_key")]
    public Guid PartnerKey { get; set; }

    public ICollection<CartOrderPartner> CartOrderPartners { get; set; }
        = new List<CartOrderPartner>();
}