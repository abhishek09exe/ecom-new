using System.Globalization;
using ecom_new_api.Data;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Services;

public class CurrencyService
{
    private readonly AppDbContext _ctx;

    public CurrencyService(AppDbContext ctx) => _ctx = ctx;

    public (string CurrencyCode, string CurrencySymbol) GetCurrency(string locale)
    {
        var parts = locale.Replace("-", "_").Split('_');
        var lang  = parts[0].ToLower();
        var iso3  = Iso2ToIso3(parts.Length > 1 ? parts[1].ToUpper() : "US");

        var entry = _ctx.CurrencyLanguageLocations
            .Include(cll => cll.Currency)
            .FirstOrDefault(cll =>
                cll.LanguageCode.ToLower() == lang &&
                cll.LocationCode.ToLower() == iso3.ToLower());

        var currencyCode = entry?.Currency?.CurrencyCode ?? "USD";
        var symbol = GetCurrencySymbol(locale, currencyCode);
        return (currencyCode, symbol);
    }

    private static string GetCurrencySymbol(string locale, string currencyCode)
    {
        try
        {
            var culture = new CultureInfo(locale.Replace("_", "-"));
            // Use the currency code's region to get the correct symbol
            var region = new RegionInfo(currencyCode);
            return region.CurrencySymbol;
        }
        catch { return "$"; }
    }

    private static string Iso2ToIso3(string iso2) => iso2 switch
    {
        "US" => "USA",
        "GB" => "GBR",
        "CA" => "CAN",
        "AU" => "AUS",
        "DE" => "DEU",
        "FR" => "FRA",
        "JP" => "JPN",
        "NL" => "NLD",
        _    => iso2.PadRight(3, 'A')
    };
}
