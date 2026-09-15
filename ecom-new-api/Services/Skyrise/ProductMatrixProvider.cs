namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// Per-license-category defaults for Skyrise keycode requests, mirroring PHP's
/// GenerateKeycode::PRODUCT_MATRIX (Section 3.3 of the Skyrise reference doc).
/// Storage and LicenseKeycodeTypeId are nullable because CBSB/OTSF/CBEP omit them in the
/// PHP source entirely (open item — how the SDK/API treats the missing keys is unconfirmed).
/// </summary>
public sealed class ProductMatrixEntry
{
    public required string LicenseCategory { get; init; }

    public required string LicenseCategoryId { get; init; }

    public required string LicenseType { get; init; }

    public int? Storage { get; init; }

    public int? LicenseKeycodeTypeId { get; init; }
}

/// <summary>
/// Provides PRODUCT_MATRIX and LICENSE_TYPE_ID_MAP lookups for Skyrise keycode requests.
/// Hidden behind an interface so a DB-backed implementation (license_category/product tables)
/// can be swapped in later without touching callers.
/// </summary>
public interface IProductMatrixProvider
{
    ProductMatrixEntry? GetProductMatrixEntry(string licenseCategory);

    int? GetLicenseTypeId(string licenseType);
}

public sealed class ProductMatrixProvider : IProductMatrixProvider
{
    // Verified, cross-confirmed stable (Section 3.3). SDNS/SECA intentionally absent —
    // handled as license modules attached post-creation, not primary Skyrise products.
    private static readonly Dictionary<string, ProductMatrixEntry> ProductMatrix = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SAEP"] = new() { LicenseCategory = "SAEP", LicenseCategoryId = "220", LicenseType = "enterprise,trial", Storage = 0, LicenseKeycodeTypeId = 3 },
        // RESOLVED: confirmed against the actual PHP source (GenerateKeycode.php::PRODUCT_MATRIX)
        // that LicenseCategoryId = "130" is correct for WIFI. The ecom QA DB's local
        // license_category.license_category_id = 135 for WIFI is an unrelated internal ID (our
        // own schema's PK for that row), not the value sent to the external Skyrise API. No
        // change needed here — this was a red herring, not a discrepancy.
        ["WIFI"] = new() { LicenseCategory = "WIFI", LicenseCategoryId = "130", LicenseType = "consumer,trial", Storage = 0, LicenseKeycodeTypeId = 1 },
        ["ADE"] = new() { LicenseCategory = "ADE", LicenseCategoryId = "55", LicenseType = "enterprise,trial", Storage = 0, LicenseKeycodeTypeId = 3 },
        ["WSAV"] = new() { LicenseCategory = "WSAV", LicenseCategoryId = "100", LicenseType = "consumer,trial", Storage = 0, LicenseKeycodeTypeId = 1 },
        ["WSAI"] = new() { LicenseCategory = "WSAI", LicenseCategoryId = "115", LicenseType = "consumer,trial", Storage = 0, LicenseKeycodeTypeId = 1 },
        ["WSAC"] = new() { LicenseCategory = "WSAC", LicenseCategoryId = "120", LicenseType = "consumer,trial", Storage = 25, LicenseKeycodeTypeId = 1 },
        // CBSB/OTSF/CBEP: no Storage/LicenseKeycodeTypeId set in the PHP source — see doc item 10.
        ["CBSB"] = new() { LicenseCategory = "CBSB", LicenseCategoryId = "200", LicenseType = "enterprise,trial", Storage = null, LicenseKeycodeTypeId = null },
        ["OTSF"] = new() { LicenseCategory = "OTSF", LicenseCategoryId = "233", LicenseType = "enterprise,trial", Storage = null, LicenseKeycodeTypeId = null },
        ["CBEP"] = new() { LicenseCategory = "CBEP", LicenseCategoryId = "234", LicenseType = "enterprise,trial", Storage = null, LicenseKeycodeTypeId = null },
        ["WE"] = new() { LicenseCategory = "WE", LicenseCategoryId = "117", LicenseType = "consumer,trial", Storage = 0, LicenseKeycodeTypeId = 1 },
    };

    // Verified, triple-cross-confirmed stable (Section 3.4 / 3.7).
    private static readonly Dictionary<string, int> LicenseTypeIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["consumer,full"] = 1,
        ["enterprise,full"] = 4,
        ["consumer,trial"] = 101,
        ["enterprise,trial"] = 104,
    };

    public ProductMatrixEntry? GetProductMatrixEntry(string licenseCategory) =>
        ProductMatrix.TryGetValue(licenseCategory, out var entry) ? entry : null;

    public int? GetLicenseTypeId(string licenseType) =>
        LicenseTypeIdMap.TryGetValue(licenseType, out var id) ? id : null;
}
