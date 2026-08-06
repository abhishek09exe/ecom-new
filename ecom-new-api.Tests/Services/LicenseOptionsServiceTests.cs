using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.LicenseOptions;
using ecom_new_api.Services;
using ecom_new_api.Services.LicenseOptions;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Services;

public sealed class LicenseOptionsServiceTests
{
    private readonly Mock<ILicenseOptionsRepository> _repoMock = new();

    private LicenseOptionsService CreateSut() => new(_repoMock.Object);

    private const string ValidGuid = "E151E1C7-018B-46EF-93A3-2CB7E01805C8";

    [Fact]
    public async Task GetLicenseOptionsByMessageKeyAsync_ResolvesKeycode_ReturnsOk()
    {
        var sut = CreateSut();
        var licenseResponse = new LicenseOptionsResponse { Keycode = "KEYCODE123" };

        _repoMock.Setup(r => r.ResolveKeycodeFromMessageKeyAsync(ValidGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync("KEYCODE123");
        _repoMock.Setup(r => r.SelectLicenseOptionsAsync("KEYCODE123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(licenseResponse);

        var result = await sut.GetLicenseOptionsByMessageKeyAsync(ValidGuid);

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Equal("KEYCODE123", result.Data!.Keycode);
    }

    [Fact]
    public async Task GetLicenseOptionsByMessageKeyAsync_MessageKeyNotFound_ReturnsNotFound()
    {
        var sut = CreateSut();

        _repoMock.Setup(r => r.ResolveKeycodeFromMessageKeyAsync(ValidGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await sut.GetLicenseOptionsByMessageKeyAsync(ValidGuid);

        Assert.Equal(ServiceResultKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task GetLicenseOptionsByMessageKeyAsync_KeycodeResolvesButLicenseNotFound_ReturnsNotFound()
    {
        var sut = CreateSut();

        _repoMock.Setup(r => r.ResolveKeycodeFromMessageKeyAsync(ValidGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync("KEYCODE123");
        _repoMock.Setup(r => r.SelectLicenseOptionsAsync("KEYCODE123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((LicenseOptionsResponse?)null);

        var result = await sut.GetLicenseOptionsByMessageKeyAsync(ValidGuid);

        Assert.Equal(ServiceResultKind.NotFound, result.Kind);
    }
}
