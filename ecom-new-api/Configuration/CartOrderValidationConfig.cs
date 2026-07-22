namespace ecom_new_api.Configuration;

/// <summary>
/// Configuration for cart order validation rules.
/// Holds allowed values that are loaded from appsettings.json at startup.
/// 
/// This replaces hardcoded static HashSets in CartOrderService.
/// </summary>
public interface ICartOrderValidationConfig
{
    /// <summary>Allowed site ID values (gsm, webroot, etc.)</summary>
    HashSet<string> AllowedSiteIds { get; }

    /// <summary>Allowed license category names (SOHO, SMB, ENT, etc.)</summary>
    HashSet<string> AllowedLicenseCategoryNames { get; }

    /// <summary>Allowed year values for license terms (1, 2, 3, etc.)</summary>
    HashSet<int> AllowedYears { get; }
}

/// <summary>
/// Standard implementation of ICartOrderValidationConfig.
/// Loads values from appsettings.json configuration section "CartOrderValidation".
/// </summary>
public class CartOrderValidationConfig : ICartOrderValidationConfig
{
    public HashSet<string> AllowedSiteIds { get; }
    public HashSet<string> AllowedLicenseCategoryNames { get; }
    public HashSet<int> AllowedYears { get; }

    public CartOrderValidationConfig(IConfiguration config)
    {
        var section = config.GetSection("CartOrderValidation");

        // Load allowed site IDs
        var siteIds = section.GetSection("AllowedSiteIds").Get<string[]>() ?? 
            new[] { "gsm", "webroot" };  // Default fallback
        AllowedSiteIds = new HashSet<string>(siteIds, StringComparer.OrdinalIgnoreCase);

        // Load allowed license categories
        var categories = section.GetSection("AllowedLicenseCategoryNames").Get<string[]>() ?? 
            new[] { "SOHO", "SMB", "ENT", "OTSF", "CBEP" };  // Default fallback
        AllowedLicenseCategoryNames = new HashSet<string>(categories, StringComparer.OrdinalIgnoreCase);

        // Load allowed years
        var years = section.GetSection("AllowedYears").Get<int[]>() ?? 
            new[] { 1, 2, 3 };  // Default fallback
        AllowedYears = new HashSet<int>(years);
    }
}
