namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// Configuration for the SkyIdentity OAuth2 client-credentials token exchange used to
/// authenticate the outer "/api/v1/bulk" Skyrise call. See Project_Info/skyrise-service-reference
/// docs/04_SKYRISE_KEYCODE_GENERATION.md Section 3.1.
/// </summary>
public sealed class SkyIdentityOptions
{
    public const string SectionName = "SkyIdentity";

    /// <summary>Token endpoint, e.g. https://skyidentity-qa.webrootcloudav.com/connect/token</summary>
    public string TokenUrl { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}
