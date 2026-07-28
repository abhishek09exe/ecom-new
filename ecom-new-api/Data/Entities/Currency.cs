using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("currency")]
public sealed class Currency
{
    [Key]
    [Column("currency_id")]
    public byte CurrencyId { get; set; }

    [Column("currency_code")]
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = default!;
}
