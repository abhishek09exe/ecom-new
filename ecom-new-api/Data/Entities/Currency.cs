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
    public string? CurrencyCode { get; set; }

    [Column("currency_description")]
    [MaxLength(20)]
    public string CurrencyDescription { get; set; } = default!;

    [Column("symbol_html")]
    [MaxLength(10)]
    public string? SymbolHtml { get; set; }

    [Column("symbol_utf8")]
    [MaxLength(10)]
    public string? SymbolUtf8 { get; set; }

    [Column("symbol_text")]
    [MaxLength(10)]
    public string? SymbolText { get; set; }
}
