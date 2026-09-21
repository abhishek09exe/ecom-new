using Microsoft.Extensions.Configuration;

namespace ecom_new_api.Services.EmailVerification;

public sealed class WebbulaOptions
{
    public const string SectionName = "Webbula";

    public string BaseUrl { get; set; } = string.Empty;

    [ConfigurationKeyName("api_key")]
    public string ApiKey { get; set; } = string.Empty;

    public string Profile { get; set; } = string.Empty;
}