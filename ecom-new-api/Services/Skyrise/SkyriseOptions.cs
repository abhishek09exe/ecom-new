namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// Configuration for the Skyrise "skyrise" connection, used both as the base host for the
/// outer bulk API and as the credentials for the inner LegacyAuthToken-signed sub-requests
/// (createkey/createmasterkey). See Project_Info/skyrise-service-reference
/// docs/04_SKYRISE_KEYCODE_GENERATION.md Sections 3.1 and 3.8.
/// </summary>
public sealed class SkyriseOptions
{
    public const string SectionName = "Skyrise";

    /// <summary>Base host for the Skyrise API, e.g. https://skyrise-qa.webrootcloudav.com</summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;
}
