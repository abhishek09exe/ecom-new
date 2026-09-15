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
/// Uses the real QA credentials — copy them from ecom-new-api/appsettings.Development.json
/// (gitignored) into environment variables before running, or edit the options below directly
/// for a one-off local run. Never commit real secrets into this file.
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

        var skyIdentityOptions = Options.Create(new SkyIdentityOptions
        {
            TokenUrl = Environment.GetEnvironmentVariable("SKYRISE_TOKEN_URL")
                ?? "https://skyidentity-qa.webrootcloudav.com/connect/token",
            ClientId = Environment.GetEnvironmentVariable("SKYRISE_CLIENT_ID") ?? "EcomConsumer",
            ClientSecret = Environment.GetEnvironmentVariable("SKYRISE_CLIENT_SECRET")
                ?? "4ddefd26-e06f-4352-b89d-5e46d7218491",
        });

        var skyriseOptions = Options.Create(new SkyriseOptions
        {
            BaseUrl = Environment.GetEnvironmentVariable("SKYRISE_BASE_URL")
                ?? "https://skyrise-qa.webrootcloudav.com",
            ClientName = Environment.GetEnvironmentVariable("SKYRISE_LEGACY_CLIENT_NAME") ?? "ThundercatsQA",
            AccessKey = Environment.GetEnvironmentVariable("SKYRISE_LEGACY_ACCESS_KEY")
                ?? "ab387584-11f8-473e-a1e8-e3a4c1784661",
            SecretKey = Environment.GetEnvironmentVariable("SKYRISE_LEGACY_SECRET_KEY")
                ?? "vZv4bKfrZA88ZAeuAzC9JZT7Xqu5xFUWuL3-mEg4EahJ",
        });

        using var tokenHttpClient = new HttpClient();
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

        using var skyriseHttpClient = new HttpClient();
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

        _output.WriteLine($"Success={result.Success}, Keycode={result.Keycode}, ErrorMessage={result.ErrorMessage}");

        // We don't assert Success == true here: the point of this test is to observe real QA
        // behavior (which may still fail while the LegacyAuthToken creds are unverified — see
        // Project_Info/SKYRISE_KEYCODE_SERVICE_SUMMARY.md follow-up #3). Assert we got a
        // well-formed, non-throwing result either way, and print it for manual inspection.
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
}
