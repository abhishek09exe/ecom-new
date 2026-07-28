namespace ecom_new_api.Data.Entities;

public sealed class Currency
{
    public byte CurrencyId { get; set; }
    public string? CurrencyCode { get; set; }        // CHAR(3) nullable in QA
    public string CurrencyDescription { get; set; } = default!; // NOT NULL
    public string? SymbolHtml { get; set; }
    public string? SymbolUtf8 { get; set; }
    public string? SymbolText { get; set; }
    public double? ExchangeRate { get; set; }
    public double? ExchangeMultiplier { get; set; }
    public string? DrLocale { get; set; }
    public byte? Active { get; set; }

    // Navigation
    public ICollection<CartOrder> CartOrders { get; set; } = [];
}
