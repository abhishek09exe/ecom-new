using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("currency_language_location")]
public class CurrencyLanguageLocation
{
    [Key]
    [Column("currency_language_location_id")]
    public int CurrencyLanguageLocationId { get; set; }

    [Column("language_code")]
    public string LanguageCode { get; set; } = string.Empty;

    [Column("location_code")]
    public string LocationCode { get; set; } = string.Empty;

    // Navigated from currency table — loaded via Include or projection
    [Column("currency_id")]
    public byte CurrencyId { get; set; }

    public Currency? Currency { get; set; }
}
