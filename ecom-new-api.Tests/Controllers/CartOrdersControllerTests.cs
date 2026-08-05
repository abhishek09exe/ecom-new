using ecom_new_api.Controllers;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using Xunit;

namespace ecom_new_api_tests.Controllers;

public sealed class CartOrdersControllerTests
{
    private readonly Mock<ICartOrderService> _serviceMock = new();

    private CartOrdersController CreateController(string remoteIp = "127.0.0.1")
    {
        var controller = new CartOrdersController(
            _serviceMock.Object,
            NullLogger<CartOrdersController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                Connection = { RemoteIpAddress = IPAddress.Parse(remoteIp) }
            }
        };

        return controller;
    }

    private static CartOrderResponse SampleOrder() => new()
    {
        CartOrderId     = 1,
        VendorOrderCode = "WR00001",
        SiteId          = "webroot",
        Locale          = "en-US",
        CurrencyCode    = "USD",
        InsertDate      = DateTime.UtcNow,
        SalesOrderDate  = DateTime.UtcNow.Date
    };

    // ── POST /cart/cart-orders ────────────────────────────────────────────────

    [Fact]
    public async Task CreateCartOrder_ServiceReturnsOk_Returns201WithData()
    {
        var order = SampleOrder();
        _serviceMock.Setup(s => s.CreateCartOrderAsync(It.IsAny<CartOrderCreateRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<CartOrderResponse>.Ok(order));

        var ctrl   = CreateController();
        var result = await ctrl.CreateCartOrder(
            new CartOrderCreateRequest { SiteId = "webroot", Locale = "en-US" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
        Assert.IsType<CartOrderResponse>(objectResult.Value);
    }

    [Fact]
    public async Task CreateCartOrder_ServiceReturnsValidationError_Returns400()
    {
        _serviceMock.Setup(s => s.CreateCartOrderAsync(It.IsAny<CartOrderCreateRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<CartOrderResponse>.Invalid(["site_id is required"]));

        var ctrl   = CreateController();
        var result = await ctrl.CreateCartOrder(
            new CartOrderCreateRequest { Locale = "en-US" },
            CancellationToken.None);

        var objectResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateCartOrder_ServiceReturnsError_Returns500()
    {
        _serviceMock.Setup(s => s.CreateCartOrderAsync(It.IsAny<CartOrderCreateRequest>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<CartOrderResponse>.Error("Database failure"));

        var ctrl   = CreateController();
        var result = await ctrl.CreateCartOrder(
            new CartOrderCreateRequest { SiteId = "webroot", Locale = "en-US" },
            CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }

    [Fact]
    public async Task CreateCartOrder_InjectsRemoteIpAddress_IntoRequest()
    {
        CartOrderCreateRequest? capturedRequest = null;
        _serviceMock.Setup(s => s.CreateCartOrderAsync(It.IsAny<CartOrderCreateRequest>(), It.IsAny<CancellationToken>()))
                    .Callback<CartOrderCreateRequest, CancellationToken>((req, _) => capturedRequest = req)
                    .ReturnsAsync(ServiceResult<CartOrderResponse>.Ok(SampleOrder()));

        var ctrl = CreateController(remoteIp: "10.0.0.5");
        await ctrl.CreateCartOrder(
            new CartOrderCreateRequest { SiteId = "webroot", Locale = "en-US" },
            CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.Equal("10.0.0.5", capturedRequest!.UserIp);
    }

    [Fact]
    public async Task CreateCartOrder_NullRemoteIp_DefaultsToAllZeros()
    {
        CartOrderCreateRequest? capturedRequest = null;
        _serviceMock.Setup(s => s.CreateCartOrderAsync(It.IsAny<CartOrderCreateRequest>(), It.IsAny<CancellationToken>()))
                    .Callback<CartOrderCreateRequest, CancellationToken>((req, _) => capturedRequest = req)
                    .ReturnsAsync(ServiceResult<CartOrderResponse>.Ok(SampleOrder()));

        var ctrl = CreateController();
        // Override with null IP
        ctrl.ControllerContext.HttpContext.Connection.RemoteIpAddress = null;

        await ctrl.CreateCartOrder(
            new CartOrderCreateRequest { SiteId = "webroot", Locale = "en-US" },
            CancellationToken.None);

        Assert.Equal("0.0.0.0", capturedRequest!.UserIp);
    }

    // ── GET /license-options ──────────────────────────────────────────────────

    private const string ValidGuid = "E151E1C7-018B-46EF-93A3-2CB7E01805C8";

    [Fact]
    public async Task GetLicenseOptions_ValidMessageKey_Returns200WithEnvelope()
    {
        var licenseResponse = new LicenseOptionsResponse { Keycode = "RESOLVED" };
        _serviceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.Ok(licenseResponse));

        var result = await CreateController().GetLicenseOptions(ValidGuid, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<LicenseOptionsResponse>>(okResult.Value);
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
    public async Task GetLicenseOptions_MessageKeyNotFound_Returns404()
    {
        _serviceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.NotFound("No license found"));

        var result = await CreateController().GetLicenseOptions(ValidGuid, null, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── GET /configure ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetConfigure_ServiceReturnsOk_Returns200WithEnvelope()
    {
        var configureResponse = new ConfigureResponse { Keycode = "KEY123" };
        _serviceMock.Setup(s => s.GetConfigureAsync("KEY123", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<ConfigureResponse>.Ok(configureResponse));

        var ctrl   = CreateController();
        var result = await ctrl.GetConfigure("KEY123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<ConfigureResponse>>(okResult.Value);
        Assert.Equal(0, envelope.ResponseCode);
        Assert.Equal("KEY123", envelope.Data!.Keycode);
    }

    [Fact]
    public async Task GetConfigure_ServiceReturnsNotFound_Returns404()
    {
        _serviceMock.Setup(s => s.GetConfigureAsync("KEY", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<ConfigureResponse>.NotFound("Not found"));

        var ctrl   = CreateController();
        var result = await ctrl.GetConfigure("KEY", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── GET /upgrade ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUpgrade_ServiceReturnsOk_Returns200WithEnvelope()
    {
        var upgradeResponse = new UpgradeResponse { Keycode = "KEY123" };
        _serviceMock.Setup(s => s.GetUpgradeAsync("KEY123", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<UpgradeResponse>.Ok(upgradeResponse));

        var ctrl   = CreateController();
        var result = await ctrl.GetUpgrade("KEY123", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<UpgradeResponse>>(okResult.Value);
        Assert.Equal(0, envelope.ResponseCode);
        Assert.Equal("KEY123", envelope.Data!.Keycode);
    }

    [Fact]
    public async Task GetUpgrade_ServiceReturnsNotFound_Returns404()
    {
        _serviceMock.Setup(s => s.GetUpgradeAsync("KEY", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<UpgradeResponse>.NotFound("Not found"));

        var ctrl   = CreateController();
        var result = await ctrl.GetUpgrade("KEY", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ── GET /license-options — new param combinations ─────────────────────────

    [Fact]
    public async Task GetLicenseOptions_ValidMessageKey_UsesMessageKeyFlow()
    {
        var response = new LicenseOptionsResponse { Keycode = "RESOLVED" };
        _serviceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.Ok(response));

        var result = await CreateController().GetLicenseOptions(ValidGuid, "en_US", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<LicenseOptionsResponse>>(ok.Value);
        Assert.Equal("RESOLVED", envelope.Data!.Keycode);
    }

    [Fact]
    public async Task GetLicenseOptions_InvalidGuidMessageKey_Returns400()
    {
        var result = await CreateController().GetLicenseOptions("not-a-guid", null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        _serviceMock.Verify(s => s.GetLicenseOptionsByMessageKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLicenseOptions_NullMessageKey_Returns400()
    {
        var result = await CreateController().GetLicenseOptions(null, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetLicenseOptions_ValidMessageKeyNotFound_Returns404()
    {
        _serviceMock.Setup(s => s.GetLicenseOptionsByMessageKeyAsync(ValidGuid, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ServiceResult<LicenseOptionsResponse>.NotFound("No license found"));

        var result = await CreateController().GetLicenseOptions(ValidGuid, null, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }
}
