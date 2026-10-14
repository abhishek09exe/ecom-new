using ecom_new_api.Data;
using ecom_new_api.Repositories.Skyrise;
using ecom_new_api.Services.Skyrise;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace ecom_new_api_tests.Services.Skyrise;

/// <summary>
/// Opt-in LIVE integration test that calls the real QA Skyrise/SkyIdentity endpoints.
/// Skipped by default (and therefore safe in normal `dotnet test` / CI runs) unless the
/// SKYRISE_LIVE_TEST environment variable is set to "1".
///
/// To run manually against QA:
///   $env:SKYRISE_LIVE_TEST = "1"
///   dotnet test --filter FullyQualifiedName~SkyriseKeycodeClientLiveTests
///
/// Requires approved QA credentials and endpoint settings through environment variables. It does
/// not fall back to committed or local configuration values.
/// </summary>
public sealed class SkyriseKeycodeClientLiveTests
{
    private readonly ITestOutputHelper _output;

    public SkyriseKeycodeClientLiveTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static bool LiveTestsEnabled =>
        Environment.GetEnvironmentVariable("SKYRISE_LIVE_TEST") == "1";

    [Fact]
    public async Task GenerateAsync_AgainstRealQAEndpoints_ReturnsKeycodeOrStructuredFailure()
    {
        if (!LiveTestsEnabled)
        {
            // xUnit has no built-in runtime-conditional skip prior to v3; early-return keeps this
            // test green/no-op unless explicitly opted into. Consider migrating to [Fact(Skip=...)]
            // with a dynamic reason string, or xunit.v3's Assert.Skip, if you prefer a visible "skipped".
            return;
        }

        var tokenUrl = GetRequiredQaEndpoint(
            "SKYRISE_TOKEN_URL", "skyidentity-qa.webrootcloudav.com", "/connect/token");
        var skyriseBaseUrl = GetRequiredQaEndpoint(
            "SKYRISE_BASE_URL", "skyrise-qa.webrootcloudav.com", "/");

        var skyIdentityOptions = Options.Create(new SkyIdentityOptions
        {
            TokenUrl = tokenUrl,
            ClientId = GetRequiredEnvironmentVariable("SKYRISE_CLIENT_ID"),
            ClientSecret = GetRequiredEnvironmentVariable("SKYRISE_CLIENT_SECRET"),
        });

        var skyriseOptions = Options.Create(new SkyriseOptions
        {
            BaseUrl = skyriseBaseUrl,
            ClientName = GetRequiredEnvironmentVariable("SKYRISE_LEGACY_CLIENT_NAME"),
            AccessKey = GetRequiredEnvironmentVariable("SKYRISE_LEGACY_ACCESS_KEY"),
            SecretKey = GetRequiredEnvironmentVariable("SKYRISE_LEGACY_SECRET_KEY"),
        });

        using var tokenHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var tokenProvider = new SkyIdentityTokenProvider(
            tokenHttpClient, skyIdentityOptions, NullLogger<SkyIdentityTokenProvider>.Instance);

        var signer = new LegacyAuthTokenSigner(skyriseOptions);
        var productMatrix = new ProductMatrixProvider();

        // In-memory SQLite so the audit-log write path is exercised without touching real QA DB.
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        using var db = new AppDbContext(dbOptions);
        db.Database.EnsureCreated();
        var failureRepository = new SkyriseFailureRepository(db, NullLogger<SkyriseFailureRepository>.Instance);

        using var skyriseHttpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var sut = new SkyriseKeycodeClient(
            skyriseHttpClient,
            skyriseOptions,
            tokenProvider,
            signer,
            productMatrix,
            failureRepository,
            NullLogger<SkyriseKeycodeClient>.Instance);

        var request = new KeycodeRequest
        {
            LicenseCategory = "WSAV", // consumer,trial — matches PRODUCT_MATRIX; TEST disti code below
            LicenseDistCode = "TEST", // per Section 3.7: 'TEST' override valid for non-production envs
            FirstName = "Live",
            LastName = "Test",
            Email = $"skyrise-live-test-{Guid.NewGuid():N}@example.com",
            DurationInDays = 30,
            Seats = 1,
            Iso = "US",
        };

        var result = await sut.GenerateAsync(request);

        _output.WriteLine($"Skyrise result: {GetSafeDiagnostic(result)}");

        // QA behavior can vary, so assert only that the client returns a structured result and
        // emit the sanitized diagnostic category above.
        Assert.NotNull(result);
        if (result.Success)
        {
            Assert.False(string.IsNullOrWhiteSpace(result.Keycode));
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));

            // Failure was audited.
            Assert.Equal(1, await db.SkyriseFailure.CountAsync());
        }
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name)?.Trim();
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Required QA setting {name} is missing.");
    }

    private static string GetRequiredQaEndpoint(string name, string approvedHost, string approvedPath)
    {
        return ValidateQaEndpoint(GetRequiredEnvironmentVariable(name), name, approvedHost, approvedPath);
    }

    private static string ValidateQaEndpoint(string value, string name, string approvedHost, string approvedPath)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps
            || !string.Equals(endpoint.IdnHost, approvedHost, StringComparison.OrdinalIgnoreCase)
            || !endpoint.IsDefaultPort
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.Equals(endpoint.AbsolutePath, approvedPath, StringComparison.Ordinal)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment))
        {
            throw new InvalidOperationException($"{name} must use the approved HTTPS QA endpoint.");
        }

        return approvedPath == "/"
            ? endpoint.GetLeftPart(UriPartial.Authority)
            : endpoint.GetLeftPart(UriPartial.Path);
    }

    [Theory]
    [InlineData("https://skyidentity-qa.webrootcloudav.com/connect/token", "SKYRISE_TOKEN_URL", "skyidentity-qa.webrootcloudav.com", "/connect/token")]
    [InlineData("https://skyrise-qa.webrootcloudav.com/", "SKYRISE_BASE_URL", "skyrise-qa.webrootcloudav.com", "/")]
    public void ValidateQaEndpoint_AcceptsApprovedQaEndpoints(
        string value, string name, string approvedHost, string approvedPath)
    {
        var validated = ValidateQaEndpoint(value, name, approvedHost, approvedPath);

        Assert.Equal(value.TrimEnd('/'), validated.TrimEnd('/'));
    }

    [Theory]
    [InlineData("https://skyrise.webrootcloudav.com/", "SKYRISE_BASE_URL", "skyrise-qa.webrootcloudav.com", "/")]
    [InlineData("http://skyrise-qa.webrootcloudav.com/", "SKYRISE_BASE_URL", "skyrise-qa.webrootcloudav.com", "/")]
    [InlineData("https://skyrise-qa.webrootcloudav.com:444/", "SKYRISE_BASE_URL", "skyrise-qa.webrootcloudav.com", "/")]
    [InlineData("https://skyrise-qa.webrootcloudav.com/other", "SKYRISE_BASE_URL", "skyrise-qa.webrootcloudav.com", "/")]
    [InlineData("https://skyrise-qa.webrootcloudav.com/?target=production", "SKYRISE_BASE_URL", "skyrise-qa.webrootcloudav.com", "/")]
    public void ValidateQaEndpoint_RejectsNonApprovedEndpoints(
        string value, string name, string approvedHost, string approvedPath)
    {
        Assert.Throws<InvalidOperationException>(
            () => ValidateQaEndpoint(value, name, approvedHost, approvedPath));
    }

    private static string GetSafeDiagnostic(KeycodeResult result)
    {
        if (result.Success)
        {
            return "success";
        }

        var category = result.ErrorMessage;
        if (category is "product_matrix_missing"
            or "license_type_id_map_missing"
            or "skyrise_response_missing_license_key"
            or "skyrise_request_failed")
        {
            return category;
        }

        const string HttpFailurePrefix = "skyrise_http_";
        if (category?.StartsWith(HttpFailurePrefix, StringComparison.Ordinal) == true
            && int.TryParse(category.AsSpan(HttpFailurePrefix.Length), out var statusCode)
            && statusCode is >= 100 and <= 599)
        {
            return $"HTTP {statusCode}";
        }

        return "skyrise_failure";
    }
}
