using System.Net.Http.Json;
using System.Text.Json;
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
        try
        {
            var matrixEntry = _productMatrix.GetProductMatrixEntry(request.LicenseCategory);
            if (matrixEntry is null)
            {
                return await FailAsync(request, "product_matrix_missing", ct);
            }

            var licenseType = request.LicenseType ?? matrixEntry.LicenseType;
            var licenseTypeId = _productMatrix.GetLicenseTypeId(licenseType);
            if (licenseTypeId is null)
            {
                return await FailAsync(request, "license_type_id_map_missing", ct);
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

            // LIVE API FINDING: sending "body" as a JSON-encoded STRING (the PHP/legacy shape)
            // produces "The model field is required." on the live endpoint, whose model type
            // (Services.Sky.SkyRise.Models.CreateKeyIntentRequestModel) doesn't exist in the PHP
            // SDK at all -- this is a newer API surface. "body" must be a nested JSON OBJECT (not
            // a string). Once sent as an object, the live endpoint bound the fields directly
            // (Seats/Storage/LicenseTypeId/etc. "field is required" errors referenced the
            // individual properties, not a wrapping "model" property) -- so no "model" wrapper is
            // needed; send innerBody directly as the object.
            object innerBodyValue = innerBody;

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
                body = innerBodyValue,
            };

            var accessToken = await _tokenProvider.GetAccessTokenAsync(ct);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/api/v1/bulk")
            {
                Content = JsonContent.Create(new[] { bulkItem }, options: JsonOptions),
            };
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);


            using var response = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var bodyEmpty = string.IsNullOrWhiteSpace(responseBody);
            JsonDocument? responseDocument = null;
            if (!bodyEmpty)
            {
                try
                {
                    responseDocument = JsonDocument.Parse(responseBody);
                }
                catch (JsonException)
                {
                }
            }

            using (responseDocument)
            {
                var jsonShape = responseDocument is null
                    ? bodyEmpty ? "empty" : "invalid-json"
                    : DescribeJsonShape(responseDocument.RootElement);
                var apiErrors = responseDocument is null
                    ? null
                    : DescribeApiErrors(responseDocument.RootElement);

                _logger.LogInformation(
                    "Skyrise HTTP response diagnostics: HttpStatusCode={HttpStatusCode} ContentType={ContentType} BodyEmpty={BodyEmpty} JsonProperties={JsonProperties} ApiErrors={ApiErrors}",
                    (int)response.StatusCode,
                    response.Content.Headers.ContentType?.ToString() ?? "missing",
                    bodyEmpty,
                    jsonShape,
                    apiErrors ?? "none");

                if (!response.IsSuccessStatusCode)
                    return await FailAsync(request, $"skyrise_http_{(int)response.StatusCode}", ct);

                if (bodyEmpty)
                    return await FailAsync(request, "skyrise_response_empty_body", ct);

                if (responseDocument is null)
                    return await FailAsync(request, "skyrise_response_invalid_json", ct);

                if (apiErrors is not null)
                    return await FailAsync(request, "skyrise_api_error", ct);

                var bulkItemFailure = InspectBulkItems(responseDocument.RootElement);
                if (bulkItemFailure is not null)
                    return await FailAsync(request, bulkItemFailure, ct);

                if (!TryFindLicenseKey(responseDocument.RootElement, out var licenseKey))
                    return await FailAsync(request, "skyrise_response_missing_license_key", ct);

                return KeycodeResult.Ok(licenseKey!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Skyrise keycode generation failed. FailureCategory={FailureCategory}; ExceptionType={ExceptionType}",
                "skyrise_request_failed",
                ex.GetType().Name);
            return await FailAsync(request, "skyrise_request_failed", ct);
        }
    }

    private static bool TryFindLicenseKey(JsonElement element, out string? licenseKey)
    {
        licenseKey = null;
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if ((string.Equals(property.Name, "licenseKey", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(property.Name, "CreateKey", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(property.Name, "CreateMasterKey", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(property.Name, "CreateMaster", StringComparison.OrdinalIgnoreCase)) &&
                        property.Value.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(property.Value.GetString()))
                    {
                        licenseKey = property.Value.GetString();
                        return true;
                    }

                    if (TryFindLicenseKey(property.Value, out licenseKey))
                        return true;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (TryFindLicenseKey(item, out licenseKey))
                        return true;
                }
                break;
            case JsonValueKind.String:
                // The bulk envelope's per-item "content" field is itself a JSON-encoded string
                // (e.g. "{\"CreateKey\":\"...\"}"), not a nested object — recurse into it if it
                // parses as JSON.
                var raw = element.GetString()?.Trim();
                if (!string.IsNullOrEmpty(raw) && raw.Length > 1 && raw[0] is '{' or '[')
                {
                    try
                    {
                        using var nested = JsonDocument.Parse(raw);
                        if (TryFindLicenseKey(nested.RootElement, out licenseKey)) return true;
                    }
                    catch (JsonException)
                    {
                        // Not JSON after all — ignore.
                    }
                }
                break;
        }

        return false;
    }

    // Bulk responses are HTTP 200 even when an item fails; returns a failure category or null.
    private string? InspectBulkItems(JsonElement root)
    {
        var items = root.ValueKind switch
        {
            JsonValueKind.Array => root.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object).ToList(),
            JsonValueKind.Object => [root],
            _ => []
        };

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            int? statusCode = TryGetPropertyIgnoreCase(item, "statusCode", out var status) &&
                              status.ValueKind == JsonValueKind.Number && status.TryGetInt32(out var code)
                ? code
                : null;
            var hasContent = TryGetPropertyIgnoreCase(item, "content", out var content);
            if (statusCode is null && !hasContent)
                continue;

            var contentFormat = "missing";
            string? contentShape = null;
            string? contentErrors = null;
            if (hasContent && content.ValueKind != JsonValueKind.String)
            {
                contentFormat = content.ValueKind.ToString();
            }
            else if (hasContent)
            {
                var raw = content.GetString()?.Trim();
                if (string.IsNullOrEmpty(raw))
                {
                    contentFormat = "empty";
                }
                else if (raw[0] is not ('{' or '['))
                {
                    contentFormat = "non-json";
                }
                else
                {
                    try
                    {
                        using var inner = JsonDocument.Parse(raw);
                        contentFormat = "json";
                        contentShape = DescribeJsonShape(inner.RootElement);
                        contentErrors = DescribeApiErrors(inner.RootElement);
                    }
                    catch (JsonException)
                    {
                        contentFormat = "invalid-json";
                    }
                }
            }

            _logger.LogInformation(
                "Skyrise bulk item diagnostics: ItemIndex={ItemIndex} ItemStatusCode={ItemStatusCode} ContentFormat={ContentFormat} ContentJsonProperties={ContentJsonProperties} ContentApiErrors={ContentApiErrors}",
                index,
                statusCode?.ToString() ?? "missing",
                contentFormat,
                contentShape ?? "none",
                contentErrors ?? "none");

            if (statusCode is < 200 or > 299)
                return $"skyrise_item_http_{statusCode}";

            if (contentErrors is not null)
                return "skyrise_api_error";
        }

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string DescribeJsonShape(JsonElement root)
    {
        const int maximumEntries = 48;
        var entries = new List<string>();
        AppendJsonShape(root, "$", 0, entries, maximumEntries);
        return string.Join(",", entries);
    }

    private static void AppendJsonShape(
        JsonElement element,
        string path,
        int depth,
        List<string> entries,
        int maximumEntries)
    {
        if (entries.Count >= maximumEntries)
            return;

        if (depth > 8)
        {
            entries.Add($"{path}:DepthLimit");
            return;
        }

        entries.Add($"{path}:{element.ValueKind}");
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var childPath = $"{path}.{SanitizePropertyName(property.Name)}";
                    AppendJsonShape(property.Value, childPath, depth + 1, entries, maximumEntries);
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray().Take(8))
                {
                    AppendJsonShape(item, $"{path}[]", depth + 1, entries, maximumEntries);
                }
                break;
        }
    }

    private static string SanitizePropertyName(string propertyName)
    {
        var safeName = new string(propertyName.Take(48)
            .Select(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.'
                ? character
                : '_')
            .ToArray());
        return string.IsNullOrEmpty(safeName) ? "[empty]" : safeName;
    }

    private static string? DescribeApiErrors(JsonElement root)
    {
        var descriptions = new List<string>();
        FindApiErrors(root, descriptions, 0);
        return descriptions.Count == 0 ? null : string.Join(";", descriptions.Take(4));
    }

    private static void FindApiErrors(JsonElement element, List<string> descriptions, int depth)
    {
        if (depth > 8 || descriptions.Count >= 4 || element.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
            return;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if ((string.Equals(property.Name, "errors", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(property.Name, "error", StringComparison.OrdinalIgnoreCase)) &&
                    HasMeaningfulValue(property.Value))
                {
                    descriptions.Add($"{SanitizePropertyName(property.Name)}:{DescribeErrorValue(property.Value)}");
                }

                FindApiErrors(property.Value, descriptions, depth + 1);
            }
        }
        else
        {
            foreach (var item in element.EnumerateArray().Take(8))
                FindApiErrors(item, descriptions, depth + 1);
        }
    }

    private static bool HasMeaningfulValue(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => false,
            JsonValueKind.String => !string.IsNullOrWhiteSpace(value.GetString()),
            JsonValueKind.Array => value.GetArrayLength() > 0,
            JsonValueKind.Object => value.EnumerateObject().Any(),
            JsonValueKind.False => false,
            _ => true
        };

    private static string DescribeErrorValue(JsonElement value)
    {
        var fields = new List<string>();
        AppendErrorFieldSummary(value, fields, 0);
        return fields.Count == 0
            ? $"{value.ValueKind}:[redacted]"
            : $"{value.ValueKind}({string.Join(",", fields.Take(8))})";
    }

    private static void AppendErrorFieldSummary(JsonElement element, List<string> fields, int depth)
    {
        if (depth > 8 || fields.Count >= 8)
            return;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var isCode = property.Name.Equals("code", StringComparison.OrdinalIgnoreCase) ||
                             property.Name.Equals("errorCode", StringComparison.OrdinalIgnoreCase) ||
                             property.Name.Equals("error_code", StringComparison.OrdinalIgnoreCase);
                var isMessage = property.Name.Equals("message", StringComparison.OrdinalIgnoreCase) ||
                                property.Name.Equals("errorMessage", StringComparison.OrdinalIgnoreCase) ||
                                property.Name.Equals("error_message", StringComparison.OrdinalIgnoreCase);
                if (isCode || isMessage)
                {
                    var label = isCode ? "code" : "message";
                    var length = property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString()?.Length ?? 0
                        : 0;
                    fields.Add($"{label}:{property.Value.ValueKind}=[redacted,length={length}]");
                }
                else
                {
                    AppendErrorFieldSummary(property.Value, fields, depth + 1);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray().Take(4))
                AppendErrorFieldSummary(item, fields, depth + 1);
        }
        else if (element.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(element.GetString()))
        {
            fields.Add($"message:String=[redacted,length={element.GetString()!.Length}]");
        }
    }

    private async Task<KeycodeResult> FailAsync(KeycodeRequest request, string errorCategory, CancellationToken ct)
    {
        var requestJson = JsonSerializer.Serialize(new { licenseCategory = request.LicenseCategory }, JsonOptions);
        var failureMessageJson = JsonSerializer.Serialize(new { category = errorCategory }, JsonOptions);
        await _failureRepository.RecordFailureAsync(requestJson, failureMessageJson, ct);
        return KeycodeResult.Failed(errorCategory);
    }
}
