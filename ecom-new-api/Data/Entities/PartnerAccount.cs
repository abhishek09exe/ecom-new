using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("partner_account")]
public sealed class PartnerAccount
{
    [Key]
    [Column("partner_account_id")]
    public int PartnerAccountId { get; set; }

    [Column("partner_id")]
    public int PartnerId { get; set; }

    [Column("account_id")]
    public int AccountId { get; set; }

    // Navigation
    public Account? Account { get; set; }
}
