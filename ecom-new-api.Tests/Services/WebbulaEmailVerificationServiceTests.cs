using System.Net;
using System.Net.Http.Json;
using System.Text;
using ecom_new_api.Services.EmailVerification;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ecom_new_api_tests.Services;

public sealed class WebbulaEmailVerificationServiceTests
{
    [Fact]
    public async Task VerifyAsync_ValidResponse_AllowsEmailAndSendsLegacyRequest()
    {
        var handler = new StubHandler(_ => JsonResponse("{\"success\":true,\"response\":{\"emails\":[{\"result\":\"Valid\"}]}}"));
        var result = await CreateSut(handler).VerifyAsync("  john@company.com  ");

        Assert.False(result.IsExplicitlyInvalid);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://webbula.example/task/express", handler.Request.RequestUri!.ToString());
        Assert.Equal("test-api-key", handler.Request.Headers.GetValues("X-API-KEY").Single());
        Assert.Equal(
            "{\"profile\":\"test-profile\",\"emails\":[\"john@company.com\"]}",
            handler.RequestBody);
    }

    [Fact]
    public async Task VerifyAsync_UnverifiedResponse_AllowsEmail()
    {
        var result = await CreateSut(new StubHandler(_ => JsonResponse("{\"success\":true,\"response\":{\"emails\":[{\"result\":\"Unverified\"}]}}")))
            .VerifyAsync("john@company.com");

        Assert.False(result.IsExplicitlyInvalid);
    }

    [Fact]
    public async Task VerifyAsync_InvalidResponse_RejectsEmail()
    {
        var result = await CreateSut(new StubHandler(_ => JsonResponse("{\"success\":true,\"response\":{\"emails\":[{\"result\":\"Invalid\"}]}}")))
            .VerifyAsync("john@company.com");

        Assert.True(result.IsExplicitlyInvalid);
    }

    [Fact]
    public async Task VerifyAsync_ServerError_AllowsEmail()
    {
        var result = await CreateSut(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)))
            .VerifyAsync("john@company.com");

        Assert.False(result.IsExplicitlyInvalid);
    }

    [Fact]
    public async Task VerifyAsync_Timeout_AllowsEmail()
    {
        var result = await CreateSut(new StubHandler(_ => throw new TaskCanceledException("timed out")))
            .VerifyAsync("john@company.com");

        Assert.False(result.IsExplicitlyInvalid);
    }

    [Fact]
    public async Task VerifyAsync_MalformedResponse_AllowsEmail()
    {
        var result = await CreateSut(new StubHandler(_ => JsonResponse("not-json")))
            .VerifyAsync("john@company.com");

        Assert.False(result.IsExplicitlyInvalid);
    }

    private static WebbulaEmailVerificationService CreateSut(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler),
            Options.Create(new WebbulaOptions
            {
                BaseUrl = "https://webbula.example",
                ApiKey = "test-api-key",
                Profile = "test-profile"
            }),
            NullLogger<WebbulaEmailVerificationService>.Instance);

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responseFactory(request);
        }
    }
}