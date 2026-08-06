using ecom_new_api.Models.Responses;
using ecom_new_api.Services;
using ecom_new_api.Services.CartOrders;
using ecom_new_api.Services.LicenseOptions;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Services;

public sealed class LicenseOptionsServiceTests
{
    private readonly Mock<ICartOrderService> _cartOrderServiceMock = new();

    private LicenseOptionsService CreateSut() => new(_cartOrderServiceMock.Object);

    private const string ValidGuid = "E151E1C7-018B-46EF-93A3-2CB7E01805C8";

    [Fact]
    public async Task GetLicenseOptionsByMessageKeyAsync_ResolvesKeycode_ReturnsOk()
    {
        var sut = CreateSut();
        var licenseResponse = new LicenseOptionsResponse { Keycode = "KEYCODE123" };

        _cartOrderServiceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.Ok(licenseResponse));

        var result = await sut.GetLicenseOptionsByMessageKeyAsync(ValidGuid);

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Equal("KEYCODE123", result.Data!.Keycode);
    }

    [Fact]
    public async Task GetLicenseOptionsByMessageKeyAsync_MessageKeyNotFound_ReturnsNotFound()
    {
        var sut = CreateSut();

        _cartOrderServiceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.NotFound("No license found"));

        var result = await sut.GetLicenseOptionsByMessageKeyAsync(ValidGuid);

        Assert.Equal(ServiceResultKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task GetLicenseOptionsByMessageKeyAsync_KeycodeResolvesButLicenseNotFound_ReturnsNotFound()
    {
        var sut = CreateSut();

        _cartOrderServiceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.NotFound("No license found"));

        var result = await sut.GetLicenseOptionsByMessageKeyAsync(ValidGuid);

        Assert.Equal(ServiceResultKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task GetLicenseOptionsByMessageKeyAsync_ForwardsLocale()
    {
        var sut = CreateSut();
        var licenseResponse = new LicenseOptionsResponse { Keycode = "KEYCODE123" };

        _cartOrderServiceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, "en-US", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.Ok(licenseResponse));

        var result = await sut.GetLicenseOptionsByMessageKeyAsync(ValidGuid, "en-US");

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        _cartOrderServiceMock.Verify(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, "en-US", It.IsAny<CancellationToken>()), Times.Once);
    }
}
