using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.Cart;
using ecom_new_api.Services;
using ecom_new_api.Services.CartOrders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Services;

public sealed class CartOrderServiceTests
{
    private readonly Mock<ICartOrderRepository> _repoMock = new();
    private readonly IConfiguration _config = new ConfigurationBuilder().Build();

    private CartOrderService CreateSut() =>
        new(_repoMock.Object, NullLogger<CartOrderService>.Instance, _config);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CartOrderCreateRequest ValidRequest(
        Action<CartOrderCreateRequest>? mutate = null)
    {
        var req = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            Items =
            [
                new CartOrderItemRequest { LicenseCategoryName = "SOHO", ProductId = 1 }
            ]
        };

        // CartOrderCreateRequest uses init-only setters, so we build it fresh if mutation is needed.
        // For simple mutation tests callers pass a different object directly.
        _ = mutate; // suppress unused-parameter warning
        return req;
    }

    private static CartOrderResponse SampleOrderResponse() => new()
    {
        CartOrderId    = 42,
        VendorOrderCode = "WR00042",
        SiteId         = "webroot",
        Locale         = "en-US",
        CurrencyCode   = "USD",
        InsertDate     = DateTime.UtcNow,
        SalesOrderDate = DateTime.UtcNow.Date,
        Items          = new Dictionary<string, List<CartOrderItemResponse>>()
    };

    // ── CreateCartOrderAsync — validation ────────────────────────────────────

    [Fact]
    public async Task CreateCartOrderAsync_MissingSiteId_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest { Locale = "en-US" };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains("site_id is required", result.ValidationErrors);
    }

    [Fact]
    public async Task CreateCartOrderAsync_MissingLocale_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest { SiteId = "webroot" };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains("locale is required", result.ValidationErrors);
    }

    [Fact]
    public async Task CreateCartOrderAsync_InvalidCurrencyCode_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            CurrencyCode = "US" // only 2 chars — must be 3
        };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.Contains("currency_code"));
    }

    [Fact]
    public async Task CreateCartOrderAsync_InvalidPartnerKey_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            PartnerKey = "not-a-guid"
        };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.Contains("partner_key"));
    }

    [Fact]
    public async Task CreateCartOrderAsync_InvalidUrlLink_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            UrlLink = "not a url"
        };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.Contains("url_link"));
    }

    [Fact]
    public async Task CreateCartOrderAsync_NegativeMessageCampaignId_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            MessageCampaignId = -1
        };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.Contains("message_campaign_id"));
    }

    [Fact]
    public async Task CreateCartOrderAsync_ItemMissingLicenseCategoryName_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            Items = [new CartOrderItemRequest { ProductId = 1 }] // no LicenseCategoryName
        };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.Contains("license_category_name"));
    }

    [Fact]
    public async Task CreateCartOrderAsync_ItemNegativeQuantity_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            Items = [new CartOrderItemRequest { LicenseCategoryName = "SOHO", ProductId = 1, Quantity = 0 }]
        };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.Contains("quantity"));
    }

    [Fact]
    public async Task CreateCartOrderAsync_ItemNegativeLicenseSeats_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            Items = [new CartOrderItemRequest { LicenseCategoryName = "SOHO", ProductId = 1, LicenseSeats = -5 }]
        };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.Contains("license_seats"));
    }

    [Fact]
    public async Task CreateCartOrderAsync_ItemInvalidHierarchyId_ReturnsValidationError()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            Items = [new CartOrderItemRequest { LicenseCategoryName = "SOHO", ProductId = 1, ItemHierarchyId = 5 }]
        };

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.Contains("item_hierarchy_id"));
    }

    [Fact]
    public async Task CreateCartOrderAsync_MultipleValidationErrors_AllReturned()
    {
        var sut = CreateSut();
        var request = new CartOrderCreateRequest(); // missing both SiteId and Locale

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.True(result.ValidationErrors.Count >= 2);
        Assert.Contains("site_id is required", result.ValidationErrors);
        Assert.Contains("locale is required", result.ValidationErrors);
    }

    // ── CreateCartOrderAsync — success path ───────────────────────────────────

    [Fact]
    public async Task CreateCartOrderAsync_ValidRequest_CallsInsertAndSelect()
    {
        var sut = CreateSut();
        var request = ValidRequest();
        var orderResponse = SampleOrderResponse();

        _repoMock.Setup(r => r.FindExistingVendorOrderCodeByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((string?)null);
        _repoMock.Setup(r => r.InsertCartOrderAsync(request, It.IsAny<CancellationToken>()))
                 .ReturnsAsync("WR00042");
        _repoMock.Setup(r => r.SelectCartOrderAsync("WR00042", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(orderResponse);

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.NotNull(result.Data);
        Assert.Equal("WR00042", result.Data!.VendorOrderCode);

        _repoMock.Verify(r => r.InsertCartOrderAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SelectCartOrderAsync("WR00042", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCartOrderAsync_ValidRequest_DoesNotCallFindKey_WhenNoMessageKey()
    {
        var sut = CreateSut();
        var request = ValidRequest(); // MessageKey is null

        _repoMock.Setup(r => r.InsertCartOrderAsync(request, It.IsAny<CancellationToken>()))
                 .ReturnsAsync("WR00042");
        _repoMock.Setup(r => r.SelectCartOrderAsync("WR00042", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(SampleOrderResponse());

        await sut.CreateCartOrderAsync(request);

        _repoMock.Verify(
            r => r.FindExistingVendorOrderCodeByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateCartOrderAsync_SelectReturnsNull_ReturnsError()
    {
        var sut = CreateSut();
        var request = ValidRequest();

        _repoMock.Setup(r => r.InsertCartOrderAsync(request, It.IsAny<CancellationToken>()))
                 .ReturnsAsync("WR00042");
        _repoMock.Setup(r => r.SelectCartOrderAsync("WR00042", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((CartOrderResponse?)null);

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.Error, result.Kind);
        Assert.False(result.IsSuccess);
    }

    // ── CreateCartOrderAsync — routing action ─────────────────────────────────

    [Fact]
    public async Task CreateCartOrderAsync_WithRoutingAction_SetsRouteOnResponse()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CartRouteBaseUrl"] = "https://www.example.com/cart"
            })
            .Build();

        var sut = new CartOrderService(_repoMock.Object, NullLogger<CartOrderService>.Instance, config);
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            RoutingAction = "autoprocess",
            MessageKey = "key123",
            Items = [new CartOrderItemRequest { LicenseCategoryName = "SOHO", ProductId = 1 }]
        };
        var orderResponse = SampleOrderResponse();

        _repoMock.Setup(r => r.FindExistingVendorOrderCodeByKeyAsync("key123", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((string?)null);
        _repoMock.Setup(r => r.InsertCartOrderAsync(request, It.IsAny<CancellationToken>()))
                 .ReturnsAsync("WR00042");
        _repoMock.Setup(r => r.SelectCartOrderAsync("WR00042", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(orderResponse);

        var result = await sut.CreateCartOrderAsync(request);

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.NotNull(result.Data!.Route);
        Assert.Contains("autoprocess", result.Data.Route!.Route);
        Assert.Contains("key123", result.Data.Route.Route);
    }

    // ── CreateCartOrderAsync — quote-key pivot path ───────────────────────────

    [Fact]
    public async Task CreateCartOrderAsync_WithExistingKey_StillCallsInsert_WhenUpdateNotImplemented()
    {
        // The service currently falls through to insert even when an existing key is found
        // (update path is TODO). This test documents that current behavior.
        var sut = CreateSut();
        var request = new CartOrderCreateRequest
        {
            SiteId = "webroot",
            Locale = "en-US",
            MessageKey = "EXISTING_KEY",
            Items = [new CartOrderItemRequest { LicenseCategoryName = "SOHO", ProductId = 1 }]
        };
        var orderResponse = SampleOrderResponse();

        _repoMock.Setup(r => r.FindExistingVendorOrderCodeByKeyAsync("EXISTING_KEY", It.IsAny<CancellationToken>()))
                 .ReturnsAsync("WR00001"); // found existing
        _repoMock.Setup(r => r.InsertCartOrderAsync(request, It.IsAny<CancellationToken>()))
                 .ReturnsAsync("WR00042");
        _repoMock.Setup(r => r.SelectCartOrderAsync("WR00042", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(orderResponse);

        var result = await sut.CreateCartOrderAsync(request);

        // Falls through to insert because update is not yet implemented
        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        _repoMock.Verify(r => r.InsertCartOrderAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetConfigureAsync ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConfigureAsync_EmptyKeycode_ReturnsValidationError(string keycode)
    {
        var sut = CreateSut();
        var result = await sut.GetConfigureAsync(keycode);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
    }

    [Fact]
    public async Task GetConfigureAsync_NotFound_ReturnsNotFound()
    {
        var sut = CreateSut();
        _repoMock.Setup(r => r.SelectConfigureAsync("KEY", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((ConfigureResponse?)null);

        var result = await sut.GetConfigureAsync("KEY");

        Assert.Equal(ServiceResultKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task GetConfigureAsync_Found_ReturnsOk()
    {
        var sut = CreateSut();
        var configResponse = new ConfigureResponse { Keycode = "KEY" };
        _repoMock.Setup(r => r.SelectConfigureAsync("KEY", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(configResponse);

        var result = await sut.GetConfigureAsync("KEY");

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Equal("KEY", result.Data!.Keycode);
    }

    // ── GetUpgradeAsync ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUpgradeAsync_EmptyKeycode_ReturnsValidationError(string keycode)
    {
        var sut = CreateSut();
        var result = await sut.GetUpgradeAsync(keycode);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
    }

    [Fact]
    public async Task GetUpgradeAsync_NotFound_ReturnsNotFound()
    {
        var sut = CreateSut();
        _repoMock.Setup(r => r.SelectUpgradeAsync("KEY", It.IsAny<CancellationToken>()))
                 .ReturnsAsync((UpgradeResponse?)null);

        var result = await sut.GetUpgradeAsync("KEY");

        Assert.Equal(ServiceResultKind.NotFound, result.Kind);
    }

    [Fact]
    public async Task GetUpgradeAsync_Found_ReturnsOk()
    {
        var sut = CreateSut();
        var upgradeResponse = new UpgradeResponse { Keycode = "KEY" };
        _repoMock.Setup(r => r.SelectUpgradeAsync("KEY", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(upgradeResponse);

        var result = await sut.GetUpgradeAsync("KEY");

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Equal("KEY", result.Data!.Keycode);
    }

}
