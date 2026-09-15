using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// Signs inner Skyrise sub-requests (createkey/createmasterkey) using the LegacyAuthToken
/// scheme, per the fully-documented algorithm in Section 3.8 of the Skyrise reference doc
/// (ported from LegacyAuthToken::_getRequestToken() in the PHP SDK).
/// </summary>
public interface ILegacyAuthTokenSigner
{
    /// <summary>
    /// Builds the "Authorisation"/"RequestToken" value for a given action.
    /// </summary>
    /// <param name="actionName">e.g. "createkey" or "createmasterkey".</param>
    /// <param name="keycode">Defaults to "noKeycode" when not applicable.</param>
    string GetRequestToken(string actionName, string keycode = "noKeycode");
}

public sealed class LegacyAuthTokenSigner : ILegacyAuthTokenSigner
{
    private readonly SkyriseOptions _options;

    public LegacyAuthTokenSigner(Microsoft.Extensions.Options.IOptions<SkyriseOptions> options)
    {
        _options = options.Value;
    }

    public string GetRequestToken(string actionName, string keycode = "noKeycode")
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        // Original (unsorted, original-case) elements — used for the final token value.
        var elems = new[] { _options.ClientName, actionName, keycode, _options.AccessKey, timestamp };

        // Signing input: lowercase every element, sort alphabetically, concatenate with no separator.
        var sorted = elems
            .Select(e => e.ToLowerInvariant())
            .OrderBy(e => e, StringComparer.Ordinal)
            .ToArray();
        var signingString = string.Concat(sorted);

        var hmacBytes = HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.SecretKey), Encoding.UTF8.GetBytes(signingString));
        var hexHmac = Convert.ToHexStringLower(hmacBytes).ToUpperInvariant();

        return $"{hexHmac},{string.Join(",", elems)}";
    }
}
