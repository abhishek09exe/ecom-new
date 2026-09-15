namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// Request DTO for generating a single Skyrise trial keycode, matching the fields sent by
/// GenerateKeycode.php (Section 3.2 of the Skyrise reference doc).
/// </summary>
public sealed class KeycodeRequest
{
    /// <summary>License category name, e.g. "SAEP", "WIFI" — used to look up PRODUCT_MATRIX defaults.</summary>
    public string LicenseCategory { get; set; } = string.Empty;

    public string LicenseDistCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int DurationInDays { get; set; }

    public int Seats { get; set; }

    /// <summary>ISO country code.</summary>
    public string Iso { get; set; } = string.Empty;

    /// <summary>
    /// "consumer,trial" or "enterprise,trial" style key into LICENSE_TYPE_ID_MAP. If not supplied,
    /// resolved from the PRODUCT_MATRIX entry's LicenseType.
    /// </summary>
    public string? LicenseType { get; set; }

    /// <summary>
    /// Module string for this license category/iso combination (e.g. "DNS=full"), matching PHP's
    /// GenerateKeycode::getTrialParams() lookup of License::module_string by
    /// (license_category_name, iso). Defaults to empty string, matching PHP's default when no
    /// module string is found — never omitted from the request body (Section 3.2/3.7).
    /// </summary>
    public string LicenseModules { get; set; } = string.Empty;
}

/// <summary>
/// Non-throwing result of a Skyrise keycode-generation attempt. A failure here is expected to be
/// non-fatal to the caller — the DB-side fallback (usp_license_generate_keycode_cart) takes over
/// when Keycode is null. See Section 3.5 / 4 of the Skyrise reference doc.
/// </summary>
public sealed class KeycodeResult
{
    public bool Success { get; init; }

    public string? Keycode { get; init; }

    public string? ErrorMessage { get; init; }

    public static KeycodeResult Ok(string keycode) => new() { Success = true, Keycode = keycode };

    public static KeycodeResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
