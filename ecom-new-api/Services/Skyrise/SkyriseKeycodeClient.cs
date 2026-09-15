using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ecom_new_api.Repositories.Skyrise;
using Microsoft.Extensions.Options;

namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// Ports GenerateKeycode.php: calls Skyrise's "/api/v1/bulk" endpoint to synchronously request a
/// trial keycode. Never throws for Skyrise-side failures — returns a KeycodeResult and logs a
/// SkyriseFailure audit row, matching the non-fatal failure model in Section 3.5 of the Skyrise
/// reference doc. The DB-side fallback (usp_license_generate_keycode_cart) is out of scope here;
/// callers are expected to fall back to it when Success is false.
/// </summary>
public interface ISkyriseKeycodeClient
{
    Task<KeycodeResult> GenerateAsync(KeycodeRequest request, CancellationToken ct = default);
}

public sealed class SkyriseKeycodeClient : ISkyriseKeycodeClient
{
    private readonly HttpClient _httpClient;
    private readonly SkyriseOptions _options;
    private readonly ISkyIdentityTokenProvider _tokenProvider;
    private readonly ILegacyAuthTokenSigner _signer;
    private readonly IProductMatrixProvider _productMatrix;
    private readonly ISkyriseFailureRepository _failureRepository;
    private readonly ILogger<SkyriseKeycodeClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public SkyriseKeycodeClient(
        HttpClient httpClient,
        IOptions<SkyriseOptions> options,
        ISkyIdentityTokenProvider tokenProvider,
        ILegacyAuthTokenSigner signer,
        IProductMatrixProvider productMatrix,
        ISkyriseFailureRepository failureRepository,
        ILogger<SkyriseKeycodeClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _tokenProvider = tokenProvider;
        _signer = signer;
        _productMatrix = productMatrix;
        _failureRepository = failureRepository;
        _logger = logger;
    }

    public async Task<KeycodeResult> GenerateAsync(KeycodeRequest request, CancellationToken ct = default)
    {
        object? bulkPayload = null;
        try
        {
            var matrixEntry = _productMatrix.GetProductMatrixEntry(request.LicenseCategory);
            if (matrixEntry is null)
            {
                return await FailAsync(request, $"No PRODUCT_MATRIX entry found for license category '{request.LicenseCategory}'.", ct);
            }

            var licenseType = request.LicenseType ?? matrixEntry.LicenseType;
            var licenseTypeId = _productMatrix.GetLicenseTypeId(licenseType);
            if (licenseTypeId is null)
            {
                return await FailAsync(request, $"No LICENSE_TYPE_ID_MAP entry found for license type '{licenseType}'.", ct);
            }

            // LicenseKeycodeTypeId == 3 => template/master key, else a regular key (Section 3.1/3.8).
            var isTemplate = matrixEntry.LicenseKeycodeTypeId == 3;
            var actionName = isTemplate ? "createmasterkey" : "createkey";
            var path = isTemplate ? "/master/createmasterkey" : "/key/createkey";

            // Wire-format rules (Section 3.7): empty-string defaults, no nulls; omit StartDate/EndDate
            // entirely since they are never populated here.
            // NOTE: field order/casing here is PascalCase to match SkyRise\RequestBody's field
            // whitelist (see Section 3.7); this is a distinct convention from the newer camelCase
            // REST fields (licenseKey, startDate, etc.) used elsewhere in the SDK — do not conflate.
            var innerBody = new Dictionary<string, string>
            {
                ["LicenseDistCode"] = request.LicenseDistCode,
                ["LicenseCategory"] = matrixEntry.LicenseCategory,
                ["Storage"] = matrixEntry.Storage?.ToString() ?? string.Empty,
                ["FirstName"] = request.FirstName,
                ["LastName"] = request.LastName,
                ["Email"] = request.Email,
                ["DurationInDays"] = request.DurationInDays.ToString(),
                ["Seats"] = request.Seats.ToString(),
                ["IsTrial"] = "true",
                ["LicenseTypeId"] = licenseTypeId.Value.ToString(),
                ["LicenseCategoryId"] = matrixEntry.LicenseCategoryId,
                ["LicenseKeycodeTypeId"] = matrixEntry.LicenseKeycodeTypeId?.ToString() ?? string.Empty,
                ["iso"] = request.Iso,
                ["TransactionRefId"] = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{Random.Shared.Next(1000, 9999)}",
                // CONFIRMED against GenerateKeycode.php::getTrialParams(): always present, empty
                // string by default (module string lookup keyed by license_category_name + iso).
                ["LicenseModules"] = request.LicenseModules,
            };

            // CONFIRMED against GenerateKeycode.php::getAdditionalParams()/isParentProduct(): for
            // master-key/template requests (LicenseKeycodeTypeId == 3), SeatCount must duplicate
            // Seats. Missing this field for SAEP (which IS LicenseKeycodeTypeId == 3) is a likely
            // contributor to live failures for that category.
            if (isTemplate)
            {
                innerBody["SeatCount"] = request.Seats.ToString();
            }

            // CONFIRMED against php/skyrise-sdk/Request.php::getData() and auth/LegacyAuthToken.php:
            // the signed token is merged INTO the body as a "RequestToken" field (not a sibling
            // bulk-item property). PHP: `$data = $tok->getRequestData() + $data;` where
            // getRequestData() returns ['RequestToken' => $this->_getRequestToken()].
            innerBody["RequestToken"] = _signer.GetRequestToken(actionName);
            var innerBodyJson = JsonSerializer.Serialize(innerBody, JsonOptions);

            // CONFIRMED against php/skyrise-sdk/Request.php::toBulk(): the bulk-item envelope is
            // { method, bulkId, path, headers, body } and "headers" is only for CUSTOM headers
            // (default empty, and stripped entirely by array_filter() if empty/"[]"). It does NOT
            // carry Content-Type — that's only added for direct, non-bulk requests. There is no
            // separate "requestToken" field at this level.
            var bulkItem = new
            {
                method = "POST",
                bulkId = Guid.NewGuid().ToString(),
                path,
                body = innerBodyJson,
            };

            bulkPayload = new[] { bulkItem };

            var accessToken = await _tokenProvider.GetAccessTokenAsync(ct);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/api/v1/bulk")
            {
                Content = JsonContent.Create(bulkPayload, options: JsonOptions),
            };
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);


            using var response = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                return await FailAsync(request, $"Skyrise bulk call returned {(int)response.StatusCode}: {responseBody}", ct, bulkPayload);
            }

            var licenseKey = ExtractLicenseKey(responseBody);
            if (licenseKey is null)
            {
                return await FailAsync(request, $"Skyrise bulk response did not contain a license key: {responseBody}", ct, bulkPayload);
            }

            return KeycodeResult.Ok(licenseKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skyrise keycode generation failed for license category {LicenseCategory}.", request.LicenseCategory);
            return await FailAsync(request, ex.Message, ct, bulkPayload);
        }
    }

    private static string? ExtractLicenseKey(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            // Response shape from the live Skyrise API error/response taxonomy is not fully confirmed
            // (see doc item 3) — this best-effort extraction looks for a top-level or nested
            // "licenseKey" property, matching SkyRise\response\License::licenseKey.
            if (TryFindLicenseKey(doc.RootElement, out var key))
            {
                return key;
            }
        }
        catch (JsonException)
        {
            // Not JSON, or unexpected shape — treated as failure by the caller.
        }

        return null;
    }

    private static bool TryFindLicenseKey(JsonElement element, out string? licenseKey)
    {
        licenseKey = null;
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (string.Equals(prop.Name, "licenseKey", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        licenseKey = prop.Value.GetString();
                        return true;
                    }
                    if (TryFindLicenseKey(prop.Value, out licenseKey)) return true;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (TryFindLicenseKey(item, out licenseKey)) return true;
                }
                break;
        }
        return false;
    }

    private async Task<KeycodeResult> FailAsync(KeycodeRequest request, string errorMessage, CancellationToken ct, object? bulkPayload = null)
    {
        var requestJson = JsonSerializer.Serialize(new { request, bulkPayload }, JsonOptions);
        var failureMessageJson = JsonSerializer.Serialize(new { error = errorMessage }, JsonOptions);
        await _failureRepository.RecordFailureAsync(requestJson, failureMessageJson, ct);
        return KeycodeResult.Failed(errorMessage);
    }
}
