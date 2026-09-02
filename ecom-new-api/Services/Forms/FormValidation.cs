using System.Globalization;
using System.Text.RegularExpressions;

namespace ecom_new_api.Services.Forms;

/// <summary>
/// Field-level validators ported from the legacy Lithium validators used by the trial forms
/// (notEmpty, email, matchingField, countryISO, inList, inRange, positiveInteger, phone).
/// </summary>
internal static class FormValidation
{
    private static readonly Regex EmailRegex =
        new(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PhoneRegex =
        new(@"^[0-9\+\-\.\(\)\s ext]{7,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> IsoCountryCodes =
        CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(c =>
            {
                try { return new RegionInfo(c.Name).TwoLetterISORegionName; }
                catch { return null; }
            })
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool NotEmpty(string? value) => !string.IsNullOrWhiteSpace(value);

    public static bool IsEmail(string? value)
        => !string.IsNullOrWhiteSpace(value) && EmailRegex.IsMatch(value.Trim());

    public static bool IsPhone(string? value)
        => !string.IsNullOrWhiteSpace(value) && PhoneRegex.IsMatch(value.Trim());

    public static bool Matches(string? value, string? compareTo)
        => string.Equals(value?.Trim(), compareTo?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool IsCountryIso(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Trim().Length == 2
           && IsoCountryCodes.Contains(value.Trim());

    public static bool InList(string? value, params string[] list)
        => !string.IsNullOrWhiteSpace(value)
           && list.Contains(value.Trim(), StringComparer.OrdinalIgnoreCase);

    public static bool IsPositiveInteger(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) && i > 0;

    public static bool InRange(string? value, int lower, int upper)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)
           && i > lower && i <= upper;

    public static int? ToInt(string? value)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : null;

    public static bool ToBool(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && (value.Equals("1", StringComparison.Ordinal)
               || value.Equals("true", StringComparison.OrdinalIgnoreCase)
               || value.Equals("on", StringComparison.OrdinalIgnoreCase)
               || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
}
