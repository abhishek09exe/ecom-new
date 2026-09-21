using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace ecom_new_api.Services.EmailVerification;

public sealed class WebbulaEmailVerificationService : IEmailVerificationService
{
    private const string VerificationPath = "/task/express";

    private readonly HttpClient _httpClient;
    private readonly WebbulaOptions _options;
    private readonly ILogger<WebbulaEmailVerificationService> _logger;

    public WebbulaEmailVerificationService(
        HttpClient httpClient,
        IOptions<WebbulaOptions> options,
        ILogger<WebbulaEmailVerificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<WebbulaVerificationResult> VerifyAsync(
        string email,
        CancellationToken ct = default)
    {
        var sanitizedEmail = email.Trim();

        if (string.IsNullOrWhiteSpace(_options.BaseUrl) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.Profile))
        {
            _logger.LogWarning("Webbula verification is not configured; allowing the email through.");
            return WebbulaVerificationResult.Allowed();
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl.TrimEnd('/')}{VerificationPath}")
            {
                Content = JsonContent.Create(new
                {
                    profile = _options.Profile,
                    emails = new[] { sanitizedEmail }
                })
            };
            request.Headers.Add("X-API-KEY", _options.ApiKey);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Webbula verification returned HTTP status {StatusCode}; allowing the email through.",
                    (int)response.StatusCode);
                return WebbulaVerificationResult.Allowed();
            }

            var result = await response.Content.ReadFromJsonAsync<WebbulaResponse>(cancellationToken: ct);
            var vendorStatus = result?.Response?.Emails?.FirstOrDefault()?.Result;

            if (result?.Success != true || string.IsNullOrWhiteSpace(vendorStatus))
            {
                _logger.LogWarning("Webbula verification returned an empty or unsuccessful response; allowing the email through.");
                return WebbulaVerificationResult.Allowed();
            }

            if (string.Equals(vendorStatus, "Invalid", StringComparison.OrdinalIgnoreCase))
                return WebbulaVerificationResult.Invalid();

            if (string.Equals(vendorStatus, "Valid", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(vendorStatus, "Unverified", StringComparison.OrdinalIgnoreCase))
            {
                return WebbulaVerificationResult.Allowed();
            }

            _logger.LogWarning(
                "Webbula verification returned an unknown vendor status {VendorStatus}; allowing the email through.",
                vendorStatus);
            return WebbulaVerificationResult.Allowed();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Webbula verification failed; allowing the email through.");
            return WebbulaVerificationResult.Allowed();
        }
    }

    private sealed class WebbulaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("response")]
        public WebbulaResponseData? Response { get; init; }
    }

    private sealed class WebbulaResponseData
    {
        [JsonPropertyName("emails")]
        public List<WebbulaEmailResult>? Emails { get; init; }
    }

    private sealed class WebbulaEmailResult
    {
        [JsonPropertyName("result")]
        public string? Result { get; init; }
    }
}