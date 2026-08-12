using ecom_new_api.Controllers;
using ecom_new_api.Models.Responses;
using ecom_new_api.Services;
using ecom_new_api.Services.LicenseOptions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Controllers;

public sealed class LicenseOptionsControllerTests
{
    private readonly Mock<ILicenseOptionsService> _serviceMock = new();

    private LicenseOptionsController CreateController()
        => new(_serviceMock.Object);

    private const string ValidGuid = "E151E1C7-018B-46EF-93A3-2CB7E01805C8";

    [Fact]
    public async Task GetLicenseOptions_ValidMessageKey_Returns200WithEnvelope()
    {
        var payload = new LicenseOptionsResponse { Keycode = "RESOLVED" };
        _serviceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.Ok(payload));

        var result = await CreateController().GetLicenseOptions(ValidGuid, null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<LicenseOptionsResponse>>(ok.Value);
        Assert.Equal(0, envelope.ResponseCode);
        Assert.Equal("RESOLVED", envelope.Data!.Keycode);
    }

    [Fact]
    public async Task GetLicenseOptions_MissingMessageKey_Returns400()
    {
        var result = await CreateController().GetLicenseOptions(null, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetLicenseOptions_InvalidGuid_Returns400()
    {
        var result = await CreateController().GetLicenseOptions("not-a-guid", null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _serviceMock.Verify(s => s.GetLicenseOptionsByMessageKeyAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLicenseOptions_NotFound_Returns404()
    {
        _serviceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, "en_US", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.NotFound("No license found"));

        var result = await CreateController().GetLicenseOptions(ValidGuid, "en_US", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
