using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Repositories.Pricing;
using ecom_new_api.Services;
using ecom_new_api.Services.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Services;

public sealed class PricingServiceGetBundlePricingTests
{
    private readonly Mock<IPricingRepository> _repoMock = new();
    private readonly Mock<MessageKeyService> _msgKeyMock;
    private readonly Mock<CurrencyService> _currencyMock;

    public PricingServiceGetBundlePricingTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>().Options;
        _msgKeyMock  = new Mock<MessageKeyService>(new AppDbContext(dbOptions), NullLogger<MessageKeyService>.Instance);
        _currencyMock = new Mock<CurrencyService>(new AppDbContext(dbOptions), NullLogger<CurrencyService>.Instance);
    }

    private PricingService CreateSut() => new(_repoMock.Object, _msgKeyMock.Object, _currencyMock.Object, NullLogger<PricingService>.Instance);

    private static ConfiguratorPricingResult MakeRow(
        string licenseCategoryName = "SAEP", int quantity = 10,
        decimal listPrice = 30m, decimal unitPrice = 24.50m, decimal usagePrice = 0m,
        byte itemHierarchyId = 1, int cartItemBundleId = 1)
        => new()
        {
            LineItem                = 1,
            Quantity                = quantity,
            ListPrice               = listPrice,
            UnitPrice               = unitPrice,
            UsagePrice              = usagePrice,
            EquivalentYearPrice     = listPrice,
            ProductDescription      = "Product",
            ProductTypeDescription  = "Type",
            LicenseCategoryName     = licenseCategoryName,
            CartItemBundleId        = cartItemBundleId,
            ItemHierarchyId         = itemHierarchyId,
        };

    private static BundlePricingRequest MakeRequest(BundlePricingItem item)
        => new()
        {
            Locale = "en_US",
            LicenseKeycodeTypeId = 1,
            Items = [item]
        };

    private void SetupDefaults()
    {
        _currencyMock.Setup(c => c.GetCurrency(It.IsAny<string>())).Returns(("USD", "$"));
        _msgKeyMock.Setup(m => m.ResolveAsync(It.IsAny<BundlePricingItem>(), It.IsAny<string>()))
            .ReturnsAsync((BundlePricingItem b, string _) => new ResolvedBundleContext { Bundle = b });
    }

    [Fact]
    public async Task GetBundlePricingAsync_PrimaryItemOnly_ReturnsMappedLineAndTotals()
    {
        SetupDefaults();
        _repoMock.Setup(r => r.GetItemPricingAsync(It.IsAny<IReadOnlyList<BundleItemPricingInput>>()))
            .ReturnsAsync([MakeRow()]);

        var item = new BundlePricingItem { LicenseCategoryName = "SAEP", LicenseSeats = 10, Years = 1 };
        var sut = CreateSut();

        var response = await sut.GetBundlePricingAsync(MakeRequest(item));

        Assert.Equal("USD", response.CurrencyCode);
        Assert.Equal("$", response.CurrencySymbol);
        Assert.Single(response.Items);
        Assert.Equal("SAEP", response.Items[0].LicenseCategoryName);
        Assert.Equal(300m, response.Totals.SubTotalListAmount);
        Assert.True(response.ProductTotals.ContainsKey("SAEP"));
    }

    [Fact]
    public async Task GetBundlePricingAsync_WithModules_PricesModulesIndependently()
    {
        SetupDefaults();
        _repoMock.SetupSequence(r => r.GetItemPricingAsync(It.IsAny<IReadOnlyList<BundleItemPricingInput>>()))
            .ReturnsAsync([MakeRow(licenseCategoryName: "SAEP", itemHierarchyId: 1)])
            .ReturnsAsync([MakeRow(licenseCategoryName: "MODULE1", itemHierarchyId: 2)]);

        var item = new BundlePricingItem
        {
            LicenseCategoryName = "SAEP",
            LicenseSeats = 10,
            Years = 1,
            Modules =
            [
                new BundleModule { LicenseCategoryName = "MODULE1", LicenseSeats = 10, Years = 1 }
            ]
        };
        var sut = CreateSut();

        var response = await sut.GetBundlePricingAsync(MakeRequest(item));

        Assert.Equal(2, response.Items.Count);
        Assert.Contains(response.Items, i => i.LicenseCategoryName == "SAEP");
        Assert.Contains(response.Items, i => i.LicenseCategoryName == "MODULE1");
        Assert.Equal(2, response.ProductTotals.Count);
        _repoMock.Verify(r => r.GetItemPricingAsync(It.IsAny<IReadOnlyList<BundleItemPricingInput>>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetBundlePricingAsync_RowWithEmptyLicenseCategoryName_IsSkipped()
    {
        SetupDefaults();
        _repoMock.Setup(r => r.GetItemPricingAsync(It.IsAny<IReadOnlyList<BundleItemPricingInput>>()))
            .ReturnsAsync([MakeRow(licenseCategoryName: "")]);

        var item = new BundlePricingItem { LicenseCategoryName = "SAEP", LicenseSeats = 10, Years = 1 };
        var sut = CreateSut();

        var response = await sut.GetBundlePricingAsync(MakeRequest(item));

        Assert.Empty(response.Items);
        Assert.Equal(0m, response.Totals.SubTotalListAmount);
    }

    [Fact]
    public async Task GetBundlePricingAsync_MultipleBundleItems_AccumulatesAcrossBundles()
    {
        SetupDefaults();
        _repoMock.SetupSequence(r => r.GetItemPricingAsync(It.IsAny<IReadOnlyList<BundleItemPricingInput>>()))
            .ReturnsAsync([MakeRow(licenseCategoryName: "SAEP", quantity: 10, listPrice: 30m, unitPrice: 24.50m)])
            .ReturnsAsync([MakeRow(licenseCategoryName: "SOHO", quantity: 5, listPrice: 20m, unitPrice: 18m)]);

        var request = new BundlePricingRequest
        {
            Locale = "en_US",
            LicenseKeycodeTypeId = 1,
            Items =
            [
                new BundlePricingItem { LicenseCategoryName = "SAEP", LicenseSeats = 10, Years = 1 },
                new BundlePricingItem { LicenseCategoryName = "SOHO", LicenseSeats = 5,  Years = 1 }
            ]
        };
        var sut = CreateSut();

        var response = await sut.GetBundlePricingAsync(request);

        Assert.Equal(2, response.Items.Count);
        Assert.Equal(300m + 100m, response.Totals.SubTotalListAmount);
        Assert.Equal(2, response.ProductTotals.Count);
    }

    [Fact]
    public async Task GetBundlePricingAsync_ResolvesLocaleAndCurrency_UsesRequestedLocale()
    {
        _currencyMock.Setup(c => c.GetCurrency("fr_FR")).Returns(("EUR", "\u20ac"));
        _msgKeyMock.Setup(m => m.ResolveAsync(It.IsAny<BundlePricingItem>(), "fr_FR"))
            .ReturnsAsync((BundlePricingItem b, string _) => new ResolvedBundleContext { Bundle = b });
        _repoMock.Setup(r => r.GetItemPricingAsync(It.IsAny<IReadOnlyList<BundleItemPricingInput>>()))
            .ReturnsAsync([MakeRow()]);

        var item = new BundlePricingItem { LicenseCategoryName = "SAEP", LicenseSeats = 10, Years = 1 };
        var request = MakeRequest(item);
        request.Locale = "fr_FR";
        var sut = CreateSut();

        var response = await sut.GetBundlePricingAsync(request);

        Assert.Equal("EUR", response.CurrencyCode);
        Assert.Equal("\u20ac", response.CurrencySymbol);
        _currencyMock.Verify(c => c.GetCurrency("fr_FR"), Times.Once);
        _msgKeyMock.Verify(m => m.ResolveAsync(It.IsAny<BundlePricingItem>(), "fr_FR"), Times.Once);
    }
}
