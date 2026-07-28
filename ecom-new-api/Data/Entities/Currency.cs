using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("currency")]
public class Currency
{
    [Key]
    [Column("currency_id")]
    public byte CurrencyId { get; set; }

    [Column("currency_code")]
    public string? CurrencyCode { get; set; }

    [Column("currency_description")]
    public string CurrencyDescription { get; set; } = string.Empty;

    [Column("symbol_html")]
    public string? SymbolHtml { get; set; }

    [Column("symbol_utf8")]
    public string? SymbolUtf8 { get; set; }

    [Column("symbol_text")]
    public string? SymbolText { get; set; }

    public ICollection<CartOrder> CartOrders { get; set; }
        = new List<CartOrder>();
}