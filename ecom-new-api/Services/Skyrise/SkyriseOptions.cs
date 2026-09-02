namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// Configuration for the SkyIdentity / SkyRise integration.
/// Mirrors the legacy `Connections::get('skyrise')` config plus
/// SkyRise\Environment and SkyIdentity\Environment host maps.
/// </summary>
public sealed class SkyriseOptions
{
    public const string SectionName = "Skyrise";

    /// <summary>When false the keycode generation call is skipped entirely.</summary>
    public bool Enabled { get; set; }

    /// <summary>"dev" or "prod" — selects the SkyRise / SkyIdentity host pair.</summary>
    public string Environment { get; set; } = "dev";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;

    public bool IsProduction
        => string.Equals(Environment, "prod", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Environment, "production", StringComparison.OrdinalIgnoreCase);

    // SkyRise\Environment::HOSTS
    public string SkyriseHost => IsProduction
        ? "https://skyrisesvc.webrootcloudav.com"
        : "https://skyrise-qa2.webrootcloudav.com";

    // SkyIdentity\Environment::HOSTS
    public string SkyIdentityHost => IsProduction
        ? "https://skyidentity.webrootcloudav.com"
        : "https://skyidentity-qa.webrootcloudav.com";
}
