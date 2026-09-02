using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// .NET port of the legacy Skyrise keycode generation flow:
///
///   wr\models\skyrise\GenerateKeycode::save()
///     -> SkyIdentity\Client::createAccessToken()          (POST /connect/token)
///     -> SkyRise\services\GenerateCombinedRequest         (POST license + PUT products + GET license)
///     -> SkyRise\services\GenerateBulkLicenses::execute() (POST /api/v1/bulk)
///     -> SkyRise\response\License                         (licenseKey / templateKey)
///
/// Failures never throw: the caller falls back to the ecom-generated keycode,
/// exactly like the legacy TrialRegistration save filter.
/// </summary>
public sealed class SkyriseKeycodeService : ISkyriseKeycodeService
{
    // GenerateKeycode::PRODUCT_MATRIX
    private static readonly Dictionary<string, ProductMatrixEntry> ProductMatrix = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SAEP"] = new("SAEP", 220, "enterprise,trial", 0, 3),
        ["WIFI"] = new("WIFI", 130, "consumer,trial", 0, 1),
        ["ADE"] = new("ADE", 55, "enterprise,trial", 0, 3),
        ["WSAV"] = new("WSAV", 100, "consumer,trial", 0, 1),
        ["WSAI"] = new("WSAI", 115, "consumer,trial", 0, 1),
        ["WSAC"] = new("WSAC", 120, "consumer,trial", 25, 1),
        ["CBSB"] = new("CBSB", 200, "enterprise,trial", 0, null),
        ["OTSF"] = new("OTSF", 233, "enterprise,trial", 0, null),
        ["CBEP"] = new("CBEP", 234, "enterprise,trial", 0, null),
        ["WE"] = new("WE", 117, "consumer,trial", 0, 1)
    };

    // GenerateKeycode::LICENSE_TYPE_ID_MAP
    private static readonly Dictionary<string, int> LicenseTypeIdMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["consumer,full"] = 1,
        ["enterprise,full"] = 4,
        ["consumer,trial"] = 101,
        ["enterprise,trial"] = 104
    };

    // TranslateProductToRequest::$products — trial categories only.
    private static readonly Dictionary<string, string[]> CategoryProducts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SAEP"] = ["wsa", "wts"],
        ["WIFI"] = ["wifi"],
        ["ADE"] = ["wsam"],
        ["WSAV"] = ["wsa", "wts"],
        ["WSAI"] = ["wsa", "wsam", "lastpass", "wts"],
        ["WSAC"] = ["wsa", "wsam", "lastpass", "wts"],
        ["WE"] = ["wsa", "wsam", "lastpass", "wts"],
        ["CBEP"] = ["wsa"],
        ["CBSB"] = [],
        ["OTSF"] = []
    };

    // RequestBody::PILLR_CATEGORIES — these suppress the GA message.
    private static readonly string[] NoGaMessageCategories = ["PLRP", "PLRM", "PLRCS", "PLRCB", "WSVPN"];

    private const int TemplateKeycodeTypeId = 3;
    private const string TypeTemplate = "templates";
    private const string TypeLicense = "licenses";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SkyriseOptions _options;
    private readonly ILogger<SkyriseKeycodeService> _logger;

    public SkyriseKeycodeService(
        IHttpClientFactory httpClientFactory,
        IOptions<SkyriseOptions> options,
        ILogger<SkyriseKeycodeService> logger)
        => (_httpClientFactory, _options, _logger) = (httpClientFactory, options.Value, logger);

    public async Task<KeycodeGenerationResult> GenerateAsync(
        KeycodeGenerationRequest request, CancellationToken ct = default)
    {
        if (!_options.Enabled)
            return KeycodeGenerationResult.Failed("Skyrise keycode generation is disabled.");

        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
            return KeycodeGenerationResult.Failed("Skyrise credentials are not configured.");

        try
        {
            var data = BuildRequestData(request);

            var token = await GetAccessTokenAsync(ct);
            if (token is null)
                return KeycodeGenerationResult.Failed("Unable to obtain a SkyIdentity access token.");

            var (bulk, returnBulkId) = BuildBulkPayload(data);
            if (bulk is null)
            {
                return KeycodeGenerationResult.Failed(
                    $"License Category {request.LicenseCategory} unable to be translated to products.");
            }

            return await SendBulkAsync(bulk, returnBulkId!, token, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skyrise keycode generation failed for category {Category}", request.LicenseCategory);
            return KeycodeGenerationResult.Failed(ex.Message);
        }
    }

    // ── GenerateKeycode::prepare / getTrialParams / getAdditionalParams ────────

    private static KeycodeData BuildRequestData(KeycodeGenerationRequest r)
    {
        var category = r.LicenseCategory?.Trim().ToUpperInvariant() ?? string.Empty;
        ProductMatrix.TryGetValue(category, out var matrix);

        var licenseType = matrix?.LicenseType;
        var keycodeTypeId = r.LicenseKeycodeTypeId ?? matrix?.LicenseKeycodeTypeId ?? 1;

        var licenseTypeId = r.LicenseTypeId;
        if (licenseTypeId is null && licenseType is not null && LicenseTypeIdMap.TryGetValue(licenseType, out var mapped))
            licenseTypeId = mapped;

        var isTemplate = keycodeTypeId == TemplateKeycodeTypeId;

        return new KeycodeData
        {
            LicenseCategory = matrix?.LicenseCategory ?? category,
            LicenseCategoryId = r.LicenseCategoryId ?? matrix?.LicenseCategoryId ?? 0,
            LicenseType = licenseType ?? string.Empty,
            LicenseTypeId = licenseTypeId ?? 0,
            LicenseKeycodeTypeId = keycodeTypeId,
            LicenseDistCode = r.LicenseDistCode ?? string.Empty,
            StorageGb = r.Storage != 0 ? r.Storage : matrix?.Storage ?? 0,
            DurationInDays = r.DurationInDays,
            Seats = r.Seats,
            // isParentProduct() copies Seats into SeatCount for template keys.
            SeatCount = isTemplate ? r.Seats : null,
            IsTrial = r.IsTrial,
            IsTemplate = isTemplate,
            LicenseModules = r.LicenseModules ?? string.Empty,
            TransactionRefId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
                + Random.Shared.Next(0, 1001).ToString()
        };
    }

    // ── SkyIdentity\Client::createAccessToken ─────────────────────────────────

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        using var client = CreateClient(_options.SkyIdentityHost);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "client_credentials"
        });

        using var response = await client.PostAsync("/connect/token", content, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("SkyIdentity token request failed with {StatusCode}", (int)response.StatusCode);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<JsonObject>(JsonOptions, ct);
        return payload?["access_token"]?.GetValue<string>();
    }

    // ── GenerateCombinedRequest::buildCreateRequest + Request::toBulk ──────────

    private static (JsonArray? Bulk, string? ReturnBulkId) BuildBulkPayload(KeycodeData data)
    {
        if (!CategoryProducts.TryGetValue(data.LicenseCategory, out var products))
            return (null, null);

        var callType = data.IsTemplate ? TypeTemplate : TypeLicense;
        // GenerateCombinedRequest uses a single generated key for every dependent path.
        var bulkKey = Guid.NewGuid().ToString("N");
        var licenseBulkId = Guid.NewGuid().ToString("N");
        var placeholder = $"{{bulkId:{licenseBulkId}}}";

        var bulk = new JsonArray
        {
            new JsonObject
            {
                ["method"] = "POST",
                ["bulkId"] = licenseBulkId,
                ["path"] = $"/api/v1/{callType}/",
                ["headers"] = BuildHeaderBody(data),
                ["body"] = BuildPostLicenseBody(data)
            }
        };

        foreach (var product in products)
        {
            bulk.Add(new JsonObject
            {
                ["method"] = "PUT",
                ["bulkId"] = Guid.NewGuid().ToString("N"),
                ["path"] = $"/api/v1/{callType}/{placeholder}/products/{product}",
                ["headers"] = BuildHeaderBody(data),
                ["body"] = BuildProductBody(product, data)
            });
        }

        // Final GET returns the license (and therefore the generated keycode).
        var returnBulkId = Guid.NewGuid().ToString("N");
        bulk.Add(new JsonObject
        {
            ["method"] = "GET",
            ["bulkId"] = returnBulkId,
            ["path"] = $"/api/v1/{callType}/{placeholder}/"
        });

        _ = bulkKey;
        return (bulk, returnBulkId);
    }

    // RequestBody::setHeaders
    private static JsonObject BuildHeaderBody(KeycodeData data)
        => new()
        {
            ["sendSkyToGaMessage"] = SendGaMessage(data) ? "true" : "false",
            ["isecomrequest"] = data.IsTemplate ? "false" : "true"
        };

    private static bool SendGaMessage(KeycodeData data)
        => !NoGaMessageCategories.Contains(data.LicenseCategory, StringComparer.OrdinalIgnoreCase);

    // request\PostLicense::translateData
    private static JsonObject BuildPostLicenseBody(KeycodeData data)
        => new()
        {
            ["templatekey"] = string.Empty,
            ["keyType"] = data.LicenseType
                .Replace(",full", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(",trial", string.Empty, StringComparison.OrdinalIgnoreCase),
            ["origin"] = string.Empty,
            ["sendSkyToGAMessage"] = SendGaMessage(data),
            ["distributionCode"] = data.LicenseDistCode
        };

    // RequestBody::translateData plus the per-product field whitelists.
    private static JsonObject BuildProductBody(string product, KeycodeData data)
    {
        var start = DateTime.UtcNow.Date;
        var startTs = new DateTimeOffset(start, TimeSpan.Zero).ToUnixTimeSeconds();
        var endTs = new DateTimeOffset(start.AddDays(data.DurationInDays), TimeSpan.Zero).ToUnixTimeSeconds();

        var body = new JsonObject
        {
            ["licenseKey"] = string.Empty,
            ["startDate"] = startTs,
            ["endDate"] = endTs,
            ["isEnabled"] = true,
            ["isTrial"] = data.IsTrial,
            ["seats"] = data.Seats,
            ["durationInDays"] = data.DurationInDays,
            ["licenseCategory"] = data.LicenseCategory,
            ["licenseCategoryId"] = data.LicenseCategoryId,
            ["sendSkyToGAMessage"] = SendGaMessage(data)
        };

        switch (product)
        {
            case "wsa":
                // PutProductWsa adds the module toggles; trials use the SkyRise defaults.
                body["skyUber"] = "N";
                body["skyCon"] = "1";
                body["skyUseMd5"] = true;
                body["skyMd5"] = true;
                body["skyUrl"] = true;
                body["skyUsePolicy"] = true;
                body["skyUsePp"] = true;
                body["skyUseSky"] = true;
                body["skyUseSkyUrl"] = true;
                body["skyUseStatus"] = true;
                body["skyUseUrlLog"] = true;
                body["storageGb"] = data.StorageGb;
                if (data.IsTemplate)
                    body["ecomConfAutoBilling"] = "disable";
                break;

            case "bas":
                return new JsonObject
                {
                    ["storageGb"] = data.StorageGb,
                    ["sync"] = string.Empty,
                    ["syncMigration"] = string.Empty
                };

            case "lastpass":
                return new JsonObject
                {
                    ["licenseKey"] = string.Empty,
                    ["isEnabled"] = true
                };

            case "wsam":
                body["mobile"] = string.Empty;
                body["parentalControls"] = string.Empty;
                break;

            case "wts":
                return new JsonObject
                {
                    ["licenseKey"] = string.Empty,
                    ["isEnabled"] = true,
                    ["skyWEbSecDrv"] = false,
                    ["skyWfDrv4Win10"] = false,
                    ["sendSkyToGAMessage"] = SendGaMessage(data)
                };
        }

        return body;
    }

    // ── GenerateBulkLicenses::send / execute ──────────────────────────────────

    private async Task<KeycodeGenerationResult> SendBulkAsync(
        JsonArray bulk, string returnBulkId, string token, CancellationToken ct)
    {
        using var client = CreateClient(_options.SkyriseHost);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var content = new StringContent(bulk.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/v1/bulk", content, ct);

        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            return KeycodeGenerationResult.Failed(
                $"Something went wrong with Bulk Send: Code {(int)response.StatusCode}. {raw}");
        }

        var results = JsonNode.Parse(raw) as JsonArray;
        if (results is null)
            return KeycodeGenerationResult.Failed("No responses returned.");

        foreach (var node in results)
        {
            if (node is not JsonObject result)
                continue;

            var status = result["statusCode"]?.GetValue<int>() ?? 0;
            var bulkId = result["bulkId"]?.GetValue<string>();

            if (status < 200 || status >= 300)
            {
                return KeycodeGenerationResult.Failed(
                    $"Something went wrong with bulk request {bulkId}: Code {status}. "
                    + (result["content"]?.ToString() ?? "Content missing!"));
            }

            if (!string.Equals(bulkId, returnBulkId, StringComparison.Ordinal))
                continue;

            // response\License: templateKey aliases into licenseKey.
            var licenseContent = result["content"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(licenseContent))
                continue;

            var license = JsonNode.Parse(licenseContent) as JsonObject;
            var keycode = license?["licenseKey"]?.GetValue<string>()
                ?? license?["templateKey"]?.GetValue<string>();

            if (!string.IsNullOrWhiteSpace(keycode))
                return KeycodeGenerationResult.Ok(keycode);
        }

        return KeycodeGenerationResult.Failed("No license key returned from SkyRise.");
    }

    private HttpClient CreateClient(string baseAddress)
    {
        var client = _httpClientFactory.CreateClient(nameof(SkyriseKeycodeService));
        client.BaseAddress = new Uri(baseAddress);
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    private sealed record ProductMatrixEntry(
        string LicenseCategory,
        int LicenseCategoryId,
        string LicenseType,
        int Storage,
        int? LicenseKeycodeTypeId);

    private sealed class KeycodeData
    {
        public string LicenseCategory { get; init; } = string.Empty;
        public int LicenseCategoryId { get; init; }
        public string LicenseType { get; init; } = string.Empty;
        public int LicenseTypeId { get; init; }
        public int LicenseKeycodeTypeId { get; init; }
        public string LicenseDistCode { get; init; } = string.Empty;
        public int StorageGb { get; init; }
        public int DurationInDays { get; init; }
        public int Seats { get; init; }
        public int? SeatCount { get; init; }
        public bool IsTrial { get; init; }
        public bool IsTemplate { get; init; }
        public string LicenseModules { get; init; } = string.Empty;
        public string TransactionRefId { get; init; } = string.Empty;
    }
}
