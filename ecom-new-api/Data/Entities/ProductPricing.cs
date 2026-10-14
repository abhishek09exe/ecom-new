using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

[Table("product_pricing")]
public sealed class ProductPricing
{
    [Key]
    [Column("product_pricing_id")]
    public int ProductPricingId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("retail_price")]
    public decimal RetailPrice { get; set; }

    [Column("edu_nfp_price")]
    public decimal? EducationNonProfitPrice { get; set; }

    [Column("govt_price")]
    public decimal? GovernmentPrice { get; set; }

    [Column("currency_id")]
    public int CurrencyId { get; set; }

    [Column("language_code")]
    [MaxLength(2)]
    public string? LanguageCode { get; set; }

    [Column("location_code")]
    [MaxLength(3)]
    public string? LocationCode { get; set; }

    // Navigation
    public Product? Product { get; set; }
}
