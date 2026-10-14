using System.Security.Cryptography;
using System.Text;
using ecom_new_api.Services.Skyrise;
using Microsoft.Extensions.Options;
using Xunit;

namespace ecom_new_api_tests.Services.Skyrise;

public sealed class LegacyAuthTokenSignerTests
{
    private static LegacyAuthTokenSigner CreateSut(SkyriseOptions options) =>
        new(Options.Create(options));

    [Fact]
    public void GetRequestToken_ProducesHexHmacFollowedByOriginalCaseUnsortedElements()
    {
        var options = new SkyriseOptions
        {
            ClientName = "test-client",
            AccessKey = "test-access-key",
            SecretKey = "test-signing-secret",
        };
        var sut = CreateSut(options);

        var token = sut.GetRequestToken("createkey");
        var parts = token.Split(',');

        // {hexHmac},{clientName},{actionName},{keycode},{accessKey},{timestamp}
        Assert.Equal(6, parts.Length);
        Assert.Equal(64, parts[0].Length); // SHA-256 hex digest length
        Assert.True(parts[0] == parts[0].ToUpperInvariant()); // uppercased hex
        Assert.Equal(options.ClientName, parts[1]);
        Assert.Equal("createkey", parts[2]);
        Assert.Equal("noKeycode", parts[3]);
        Assert.Equal(options.AccessKey, parts[4]);
        Assert.True(long.TryParse(parts[5], out _));
    }

    [Fact]
    public void GetRequestToken_SignatureMatchesManuallyComputedHmac()
    {
        var options = new SkyriseOptions
        {
            ClientName = "client",
            AccessKey = "access",
            SecretKey = "secret",
        };
        var sut = CreateSut(options);

        var token = sut.GetRequestToken("createkey", "ABC123");
        var parts = token.Split(',');
        var timestamp = parts[5];

        var elems = new[] { options.ClientName, "createkey", "ABC123", options.AccessKey, timestamp };
        var sorted = elems.Select(e => e.ToLowerInvariant()).OrderBy(e => e, StringComparer.Ordinal);
        var signingString = string.Concat(sorted);
        var expectedHmac = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.SecretKey), Encoding.UTF8.GetBytes(signingString)))
            .ToUpperInvariant();

        Assert.Equal(expectedHmac, parts[0]);
    }

    [Fact]
    public void GetRequestToken_DefaultsKeycodeToNoKeycode()
    {
        var sut = CreateSut(new SkyriseOptions { ClientName = "c", AccessKey = "a", SecretKey = "s" });

        var token = sut.GetRequestToken("createmasterkey");

        Assert.Contains(",noKeycode,", token);
    }
}
