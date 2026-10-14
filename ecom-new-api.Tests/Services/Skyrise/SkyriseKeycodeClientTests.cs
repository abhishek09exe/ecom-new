using System.Net;
using ecom_new_api.Repositories.Skyrise;
using ecom_new_api.Services.Skyrise;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Services.Skyrise;

public sealed class SkyriseKeycodeClientTests
{
    private const string SensitiveLicenseKey = "LICENSE-KEY-MUST-NOT-LOG";
    private const string ApiErrorCode = "API-ERROR-CODE-MUST-NOT-LOG";
    private const string ApiErrorMessage = "API error message must not be logged";

    [Fact]
    public async Task GenerateAsync_ExpectedLicenseResponseReturnsKeyWithoutLoggingIt()
    {
        var (client, handler, logger, _) = CreateClient(HttpStatusCode.OK,
            $"{{\"licenseKey\":\"{SensitiveLicenseKey}\"}}");

        var result = await client.GenerateAsync(CreateRequest());

        Assert.True(result.Success);
        Assert.Equal(SensitiveLicenseKey, result.Keycode);
        Assert.Equal(1, handler.CallCount);
        var logs = string.Join("\n", logger.Messages);
        Assert.Contains("HttpStatusCode=200", logs);
        Assert.Contains("ContentType=application/json", logs);
        Assert.Contains("BodyEmpty=False", logs);
        Assert.Contains("$.licenseKey:String", logs);
        Assert.DoesNotContain(SensitiveLicenseKey, logs, StringComparison.Ordinal);
        Assert.DoesNotContain("access-token-test-value", logs, StringComparison.Ordinal);
        Assert.DoesNotContain("request-token-test-value", logs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_MissingLicenseKeyReturnsFailure()
    {
        var (client, handler, logger, failures) = CreateClient(HttpStatusCode.OK,
            "{\"requestId\":\"opaque-value\",\"status\":\"complete\"}");

        var result = await client.GenerateAsync(CreateRequest());

        Assert.False(result.Success);
        Assert.Equal("skyrise_response_missing_license_key", result.ErrorMessage);
        Assert.Equal(1, handler.CallCount);
        Assert.Contains("$.requestId:String", string.Join("\n", logger.Messages));
        failures.Verify(repository => repository.RecordFailureAsync(
            It.IsAny<string>(), It.Is<string>(message => message.Contains("skyrise_response_missing_license_key")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_UnexpectedJsonStructureDoesNotProduceKeycode()
    {
        var (client, handler, _, _) = CreateClient(HttpStatusCode.OK,
            "{\"result\":{\"key\":\"not-the-documented-licenseKey-field\"}}");

        var result = await client.GenerateAsync(CreateRequest());

        Assert.False(result.Success);
        Assert.Null(result.Keycode);
        Assert.Equal("skyrise_response_missing_license_key", result.ErrorMessage);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GenerateAsync_ApiErrorsOnSuccessfulHttpStatusReturnFailureAndRedactedDiagnostics()
    {
        var responseBody = $$"""
            {
              "errors": [
                { "code": "{{ApiErrorCode}}", "message": "{{ApiErrorMessage}}" }
              ]
            }
            """;
        var (client, handler, logger, _) = CreateClient(HttpStatusCode.OK, responseBody);

        var result = await client.GenerateAsync(CreateRequest());

        Assert.False(result.Success);
        Assert.Equal("skyrise_api_error", result.ErrorMessage);
        Assert.Equal(1, handler.CallCount);
        var logs = string.Join("\n", logger.Messages);
        Assert.Contains("HttpStatusCode=200", logs);
        Assert.Contains("errors:Array", logs);
        Assert.Contains("code:String=[redacted", logs);
        Assert.Contains("message:String=[redacted", logs);
        Assert.Contains("ApiErrors=errors:Array", logs);
        Assert.DoesNotContain(ApiErrorCode, logs, StringComparison.Ordinal);
        Assert.DoesNotContain(ApiErrorMessage, logs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_EmptyBodyReturnsFailureAndDiagnostics()
    {
        var (client, handler, logger, _) = CreateClient(HttpStatusCode.OK, string.Empty);

        var result = await client.GenerateAsync(CreateRequest());

        Assert.False(result.Success);
        Assert.Equal("skyrise_response_empty_body", result.ErrorMessage);
        Assert.Equal(1, handler.CallCount);
        var logs = string.Join("\n", logger.Messages);
        Assert.Contains("BodyEmpty=True", logs);
        Assert.Contains("JsonProperties=empty", logs);
    }

    [Fact]
    public async Task GenerateAsync_MalformedBodyReturnsFailureAndDiagnostics()
    {
        var (client, handler, logger, _) = CreateClient(HttpStatusCode.OK, "{not-json");

        var result = await client.GenerateAsync(CreateRequest());

        Assert.False(result.Success);
        Assert.Equal("skyrise_response_invalid_json", result.ErrorMessage);
        Assert.Equal(1, handler.CallCount);
        Assert.Contains("JsonProperties=invalid-json", string.Join("\n", logger.Messages));
    }

    [Fact]
    public async Task GenerateAsync_HttpFailureLogsStatusAndDoesNotRetry()
    {
        var (client, handler, logger, _) = CreateClient(HttpStatusCode.BadGateway, "{\"errors\":[]}");

        var result = await client.GenerateAsync(CreateRequest());

        Assert.False(result.Success);
        Assert.Equal("skyrise_http_502", result.ErrorMessage);
        Assert.Equal(1, handler.CallCount);
        Assert.Contains("HttpStatusCode=502", string.Join("\n", logger.Messages));
    }

    private static KeycodeRequest CreateRequest() => new()
    {
        LicenseCategory = "WSAV",
        FirstName = "Test",
        LastName = "User",
        Email = "test@example.invalid",
        DurationInDays = 30,
        Seats = 1,
        Iso = "US"
    };

    private static (SkyriseKeycodeClient Client, StubHandler Handler, CapturingLogger Logger,
        Mock<ISkyriseFailureRepository> Failures) CreateClient(HttpStatusCode statusCode, string body)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(handler);
        var tokenProvider = new Mock<ISkyIdentityTokenProvider>();
        tokenProvider.Setup(provider => provider.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token-test-value");
        var signer = new Mock<ILegacyAuthTokenSigner>();
        signer.Setup(auth => auth.GetRequestToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("request-token-test-value");
        var failures = new Mock<ISkyriseFailureRepository>();
        failures.Setup(repository => repository.RecordFailureAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var logger = new CapturingLogger();
        var client = new SkyriseKeycodeClient(
            httpClient,
            Options.Create(new SkyriseOptions { BaseUrl = "https://skyrise.test" }),
            tokenProvider.Object,
            signer.Object,
            new ProductMatrixProvider(),
            failures.Object,
            logger);

        return (client, handler, logger, failures);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class CapturingLogger : ILogger<SkyriseKeycodeClient>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => EmptyScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));

        private sealed class EmptyScope : IDisposable
        {
            public static EmptyScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}