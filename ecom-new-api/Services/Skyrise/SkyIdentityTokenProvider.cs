using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// Obtains and caches a SkyIdentity OAuth2 client-credentials access token used to authenticate
/// the outer "/api/v1/bulk" Skyrise call. See Section 3.1 of the Skyrise reference doc.
/// </summary>
public interface ISkyIdentityTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
}

public sealed class SkyIdentityTokenProvider : ISkyIdentityTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly SkyIdentityOptions _options;
    private readonly ILogger<SkyIdentityTokenProvider> _logger;

    private string? _cachedToken;
    private DateTimeOffset _cachedTokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SkyIdentityTokenProvider(
        HttpClient httpClient,
        IOptions<SkyIdentityOptions> options,
        ILogger<SkyIdentityTokenProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Small safety margin so we don't hand out a token that expires mid-flight.
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _cachedTokenExpiry.AddSeconds(-30))
        {
            return _cachedToken;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _cachedTokenExpiry.AddSeconds(-30))
            {
                return _cachedToken;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.ClientSecret,
                    ["grant_type"] = "client_credentials",
                })
            };

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<SkyIdentityTokenResponse>(cancellationToken: ct)
                ?? throw new InvalidOperationException("SkyIdentity token response was empty.");

            _cachedToken = payload.AccessToken;
            _cachedTokenExpiry = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn);

            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed class SkyIdentityTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
