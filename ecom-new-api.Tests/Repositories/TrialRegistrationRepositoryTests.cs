using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Repositories.Trials;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using Xunit;

namespace ecom_new_api_tests.Repositories;

public sealed class TrialRegistrationRepositoryTests : IDisposable
{
    private const int SaepProductId = 152200130;

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public TrialRegistrationRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void RegistrationCommand_UsesOnlySupportedProcedureParameters()
    {
        var addParameters = typeof(TrialRegistrationRepository).GetMethod(
            "AddParameters", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(addParameters);

        using var command = new SqlCommand();
        var request = new MultiTrialSubmissionRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            CustomerEmail = "ada@example.invalid",
            Country = "US",
            LicenseCategoryName = "SAEP",
            LicenseSeats = 1
        };

        addParameters!.Invoke(null, [command, request, "{}"]);

        var existingProcedureParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "@first_name", "@last_name", "@company_name", "@company_type_name", "@phone_number",
            "@customer_email", "@address_1", "@address_2", "@city", "@state", "@postal_code",
            "@country", "@language_code", "@location_code", "@license_category_name", "@license_seats",
            "@years", "@trial_days", "@license_keycode_type_id", "@partner_key", "@account_user_name",
            "@partner_account_code", "@sfdc_lead_id", "@sfdc_opportunity_id", "@sfdc_trial_id",
            "@salesforce_campaign_id", "@partner_product_id", "@sfdc_distributor_id", "@sfdc_reseller_id",
            "@eloqua_parameters", "@license_distribution_method_code", "@salesforce_license_id",
            "@trial_registration_json", "@opt_in"
        };

        Assert.Equal(33, command.Parameters.Count);
        Assert.All(command.Parameters.Cast<SqlParameter>(), parameter =>
            Assert.Contains(parameter.ParameterName, existingProcedureParameters));
        Assert.Contains(command.Parameters.Cast<SqlParameter>(), parameter =>
            parameter.ParameterName == "@trial_registration_json");
        Assert.Contains(command.Parameters.Cast<SqlParameter>(), parameter =>
            parameter.ParameterName == "@opt_in");
        Assert.DoesNotContain(command.Parameters.Cast<SqlParameter>(), parameter =>
            parameter.ParameterName == "@selected_product_id");
        Assert.Equal("SAEP", command.Parameters["@license_category_name"].Value);
        Assert.Equal(1, command.Parameters["@license_seats"].Value);
        Assert.Equal(30, command.Parameters["@trial_days"].Value);
        Assert.Equal(3, command.Parameters["@license_keycode_type_id"].Value);
        Assert.Equal("en", command.Parameters["@language_code"].Value);
        Assert.Equal("us", command.Parameters["@location_code"].Value);
    }

    [Fact]
    public async Task SelectTrialProductAsync_SaepEnUsa30DaysAndType3_ReturnsQaProduct()
    {
        await SeedQaProductAsync(includePricing: true);
        await using var context = CreateContext();

        var selection = await CreateRepository(context).SelectTrialProductAsync(
            "SAEP", "EN", "USA", 30, 3);

        Assert.NotNull(selection);
        Assert.Equal(SaepProductId, selection!.ProductId);
        Assert.Equal(300, selection.ProductLineId);
        Assert.Equal(220, selection.LicenseCategoryId);
        Assert.Equal(2, selection.CapabilityId);
        Assert.Equal(30, selection.CapabilityActivationDays);
        Assert.Equal(12.50m, selection.RetailPrice);
        Assert.Equal("USD", selection.CurrencyCode);
    }

    [Fact]
    public async Task SelectTrialProductAsync_WrongKeycodeType_ReturnsNull()
    {
        await SeedQaProductAsync(includePricing: false);
        await using var context = CreateContext();

        var selection = await CreateRepository(context).SelectTrialProductAsync(
            "SAEP", "EN", "USA", 30, 1);

        Assert.Null(selection);
    }

    [Fact]
    public async Task SelectTrialProductAsync_WrongTrialDays_ReturnsNull()
    {
        await SeedQaProductAsync(includePricing: false);
        await using var context = CreateContext();

        var selection = await CreateRepository(context).SelectTrialProductAsync(
            "SAEP", "EN", "USA", 14, 3);

        Assert.Null(selection);
    }

    [Fact]
    public async Task SelectTrialProductAsync_MissingPricing_DoesNotPreventSelection()
    {
        await SeedQaProductAsync(includePricing: false);
        await using var context = CreateContext();

        var selection = await CreateRepository(context).SelectTrialProductAsync(
            "SAEP", "EN", "USA", 30, 3);

        Assert.NotNull(selection);
        Assert.Equal(SaepProductId, selection!.ProductId);
        Assert.Null(selection.RetailPrice);
        Assert.Null(selection.CurrencyId);
        Assert.Null(selection.CurrencyCode);
    }

    [Fact]
    public async Task SelectTrialProductAsync_NoMatchingCategory_ReturnsNull()
    {
        await SeedQaProductAsync(includePricing: false);
        await using var context = CreateContext();

        var selection = await CreateRepository(context).SelectTrialProductAsync(
            "NOT-A-CATEGORY", "EN", "USA", 30, 3);

        Assert.Null(selection);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_NoEligibleCampaignDiscount_LeavesSupplementalDiscountZero()
    {
        await using var context = CreateContext();
        await SeedCampaignDiscountAsync(context, 41, 20d, enabled: false);

        var result = await CreateRepository(context).ApplyTrialDiscountsAsync(
            PricedProduct(), 1, currentDateTime: CampaignNow);

        Assert.Null(result.AppliedCampaignDiscountId);
        Assert.Null(result.LegacyCartDiscountId);
        Assert.Null(result.AppliedTierDiscountId);
        Assert.Equal(0m, result.SupplementalDiscountPerItem);
        Assert.Equal(0m, result.StandardDiscountPerItem);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_EligibleCampaignDiscount_IsSupplementalAndUsesOneSeatLookup()
    {
        await using var context = CreateContext();
        await SeedCampaignDiscountAsync(context, 42, 20d);

        var result = await CreateRepository(context).ApplyTrialDiscountsAsync(
            PricedProduct(), 3, currentDateTime: CampaignNow);

        Assert.Equal(42, result.AppliedCampaignDiscountId);
        Assert.Equal(42, result.LegacyCartDiscountId);
        Assert.Null(result.AppliedTierDiscountId);
        Assert.Equal(20m, result.SupplementalDiscountPerItem);
        Assert.Equal(60m, result.ExtendedSupplementalDiscount);
        Assert.Equal(240m, result.NetPrice);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_HighestEligibleCampaignDiscountWins()
    {
        await using var context = CreateContext();
        await SeedCampaignDiscountAsync(context, 43, 10d);
        await SeedCampaignDiscountAsync(context, 44, 25d);

        var result = await CreateRepository(context).ApplyTrialDiscountsAsync(
            PricedProduct(), 1, currentDateTime: CampaignNow);

        Assert.Equal(44, result.AppliedCampaignDiscountId);
        Assert.Equal(44, result.LegacyCartDiscountId);
        Assert.Equal(25m, result.SupplementalDiscountPerItem);
        Assert.Equal(75m, result.NetPrice);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_SelectsTypeFourTierDiscountAsStandardDiscount()
    {
        await using var context = CreateContext();
        await SeedTierDiscountAsync(context, 51, 10d, low: 1, high: 5);

        var result = await CreateRepository(context).ApplyTrialDiscountsAsync(
            PricedProduct(), 3, currentDateTime: CampaignNow);

        Assert.Equal(51, result.AppliedTierDiscountId);
        Assert.Null(result.AppliedCampaignDiscountId);
        Assert.Null(result.LegacyCartDiscountId);
        Assert.Equal(10m, result.StandardDiscountPerItem);
        Assert.Equal(30m, result.ExtendedStandardDiscount);
        Assert.Equal(270m, result.NetPrice);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_TierSeatRangeIncludesLowAndExcludesHigh()
    {
        await using var context = CreateContext();
        await SeedTierDiscountAsync(context, 52, 10d, low: 1, high: 5);
        var repository = CreateRepository(context);

        var lowBoundary = await repository.ApplyTrialDiscountsAsync(
            PricedProduct(), 1, currentDateTime: CampaignNow);
        var highBoundary = await repository.ApplyTrialDiscountsAsync(
            PricedProduct(), 5, currentDateTime: CampaignNow);

        Assert.Equal(52, lowBoundary.AppliedTierDiscountId);
        Assert.Null(highBoundary.AppliedTierDiscountId);
        Assert.Equal(0m, highBoundary.StandardDiscountPerItem);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_NoCampaignOrTierDiscount_ReturnsFullNetPrice()
    {
        await using var context = CreateContext();

        var result = await CreateRepository(context).ApplyTrialDiscountsAsync(
            PricedProduct(), 2, currentDateTime: CampaignNow);

        Assert.Null(result.AppliedCampaignDiscountId);
        Assert.Null(result.AppliedTierDiscountId);
        Assert.Equal(0m, result.StandardDiscountPerItem);
        Assert.Equal(0m, result.SupplementalDiscountPerItem);
        Assert.Equal(200m, result.NetPrice);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_HighestApplicableTierDiscountWins()
    {
        await using var context = CreateContext();
        await SeedTierDiscountAsync(context, 53, 10d, low: 1, high: 5);
        await SeedTierDiscountAsync(context, 54, 25d, low: 1, high: 5);

        var result = await CreateRepository(context).ApplyTrialDiscountsAsync(
            PricedProduct(), 2, currentDateTime: CampaignNow);

        Assert.Equal(54, result.AppliedTierDiscountId);
        Assert.Equal(25m, result.StandardDiscountPerItem);
        Assert.Equal(150m, result.NetPrice);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_UsesJpyWholeUnitRoundingForStandardDiscount()
    {
        await using var context = CreateContext();
        await SeedTierDiscountAsync(context, 55, 12.346d, low: 1, high: 5);

        var result = await CreateRepository(context).ApplyTrialDiscountsAsync(
            PricedProduct(currencyCode: "JPY"), 1, currentDateTime: CampaignNow);

        Assert.Equal(12m, result.StandardDiscountPerItem);
        Assert.Equal(88m, result.NetPrice);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_SelectedZeroYearTrialUsesFullRetailPriceForTierDiscount()
    {
        await SeedQaProductAsync(includePricing: true);
        await using var context = CreateContext();
        await SeedTierDiscountAsync(context, 56, 10d, low: 1, high: 5, years: 0);
        var repository = CreateRepository(context);
        var trialProduct = await repository.SelectTrialProductAsync("SAEP", "EN", "USA", 30, 3);

        Assert.NotNull(trialProduct);
        Assert.Equal(0d, trialProduct!.Years);

        var result = await repository.ApplyTrialDiscountsAsync(
            trialProduct, 1, currentDateTime: CampaignNow);

        Assert.Equal(1.25m, result.StandardDiscountPerItem);
        Assert.Equal(11.25m, result.NetPrice);
    }

    [Fact]
    public async Task ApplyTrialDiscountsAsync_SelectedZeroYearJpyTrialUsesWholeUnitRounding()
    {
        await SeedQaProductAsync(includePricing: true, currencyCode: "JPY");
        await using var context = CreateContext();
        await SeedTierDiscountAsync(context, 57, 12.346d, low: 1, high: 5, years: 0);
        var repository = CreateRepository(context);
        var trialProduct = await repository.SelectTrialProductAsync("SAEP", "EN", "USA", 30, 3);

        Assert.NotNull(trialProduct);
        Assert.Equal(0d, trialProduct!.Years);

        var result = await repository.ApplyTrialDiscountsAsync(
            trialProduct, 1, currentDateTime: CampaignNow);

        Assert.Equal(2m, result.StandardDiscountPerItem);
        Assert.Equal(10.50m, result.NetPrice);
    }

    [Theory]
    [InlineData("SDNS")]
    [InlineData("SECA")]
    public async Task VerifyPersistedModulesAsync_RequiresAllConfiguredModuleRecords(string category)
    {
        await using var context = CreateContext();
        context.License.Add(new License { LicenseId = 7, Keycode = "LOCAL-TEST", ProductLineId = 300 });
        context.LicenseCategory.Add(new LicenseCategory { LicenseCategoryId = 221, LicenseCategoryName = category });
        context.LicenseModuleType.Add(new LicenseModuleType { LicenseModuleTypeId = 2, LicenseModuleTypeName = "trial" });
        context.LicenseModule.AddRange(
            new LicenseModule { LicenseModuleId = 10, LicenseModuleClassId = 3 },
            new LicenseModule { LicenseModuleId = 11, LicenseModuleClassId = 3 });
        context.LicenseModuleCategory.AddRange(
            new LicenseModuleCategory { LicenseCategoryId = 221, LicenseModuleId = 10 },
            new LicenseModuleCategory { LicenseCategoryId = 221, LicenseModuleId = 11 });
        await context.SaveChangesAsync();
        var repository = CreateRepository(context);
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            license_category = new[] { new { license_category_name = category, item_hierarchy_id = 2,
                license_seats = 5, start_date = "2026-10-15", expiration_date = "2026-11-14" } }
        });

        Assert.False(await repository.VerifyPersistedModulesAsync(7, json));
        foreach (var moduleId in new byte[] { 10, 11 })
        {
            context.LicenseModuleLicense.Add(new LicenseModuleLicense
            {
                LicenseId = 7, LicenseModuleId = moduleId, LicenseModuleTypeId = 2, ModuleSeats = 5,
                StartDate = new DateTime(2026, 10, 15), EndDate = new DateTime(2026, 11, 14), LicenseStatusId = 1
            });
        }
        await context.SaveChangesAsync();
        Assert.True(await repository.VerifyPersistedModulesAsync(7, json));

        var persisted = await context.LicenseModuleLicense.FirstAsync();
        persisted.ModuleSeats = 4;
        await context.SaveChangesAsync();
        Assert.False(await repository.VerifyPersistedModulesAsync(7, json));
        persisted.ModuleSeats = 5;
        persisted.EndDate = new DateTime(2026, 11, 13);
        await context.SaveChangesAsync();
        Assert.False(await repository.VerifyPersistedModulesAsync(7, json));
    }

    [Fact]
    public async Task VerifyPersistedModulesAsync_NoSelectedModulesDoesNotRequireModuleConfiguration()
    {
        await using var context = CreateContext();
        var repository = CreateRepository(context);
        Assert.True(await repository.VerifyPersistedModulesAsync(7, "{\"license_category\":[]}"));
        Assert.False(await repository.VerifyPersistedModulesAsync(7, "{}"));
    }

    private AppDbContext CreateContext() => new(_options);

    private static TrialRegistrationRepository CreateRepository(AppDbContext context) =>
        new(context, NullLogger<TrialRegistrationRepository>.Instance, new LicenseServiceStagingRepository(context));

    private static readonly DateTime CampaignNow = new(2026, 10, 5, 12, 0, 0, DateTimeKind.Local);

    private static TrialProductSelection PricedProduct(string currencyCode = "USD", double years = 1) => new()
    {
        ProductId = SaepProductId,
        ProductDescription = "OpenText Core Endpoint Protection 30 Day Trial",
        ProductTypeId = 1,
        LicenseCategoryName = "SAEP",
        LicenseCategoryId = 220,
        ProductLineId = 300,
        LanguageCode = "EN",
        LocationCode = "USA",
        Years = years,
        CapabilityId = 2,
        CapabilityTypeId = 2,
        CapabilityActivationDays = 30,
        RetailPrice = 100m,
        CurrencyCode = currencyCode
    };

    private static async Task SeedCampaignDiscountAsync(
        AppDbContext context,
        int discountId,
        double percent,
        bool enabled = true,
        byte platformId = 8,
        byte campaignClassId = 7,
        byte discountStatusId = 1,
        byte discountTypeId = 1,
        byte discountMethodId = 3,
        int lookupSeats = 1,
        double years = 1)
    {
        context.CartDiscount.Add(new CartDiscount
        {
            CartDiscountId = discountId,
            CartDiscountTypeId = discountTypeId,
            CartDiscountStatusId = discountStatusId
        });
        context.CartDiscountItem.Add(new CartDiscountItem
        {
            CartDiscountItemId = discountId,
            CartDiscountId = discountId,
            CartDiscountMethodId = discountMethodId,
            Discount = percent,
            ProductTypeId = 1,
            ProductLineId = 300,
            LicenseCategoryId = 220,
            LicenseCategoryName = "SAEP",
            LicenseSeats = lookupSeats,
            Years = years
        });
        context.MessageCampaign.Add(new MessageCampaign
        {
            MessageCampaignId = discountId,
            MessageCampaignClassId = campaignClassId,
            MessageCampaignEnabled = enabled ? (byte)1 : (byte)0,
            MessageCampaignStartDate = CampaignNow.AddDays(-1),
            MessageCampaignEndDate = CampaignNow.AddDays(1)
        });
        context.MessageCampaignPlatform.Add(new MessageCampaignPlatform
        {
            MessageCampaignPlatformId = discountId,
            MessageCampaignId = discountId,
            MessagePlatformId = platformId
        });
        context.MessageCampaignCartDiscount.Add(new MessageCampaignCartDiscount
        {
            MessageCampaignCartDiscountId = discountId,
            MessageCampaignId = discountId,
            CartDiscountId = discountId
        });
        context.MessageCampaignLanguageLocation.Add(new MessageCampaignLanguageLocation
        {
            MessageCampaignLanguageLocationId = discountId,
            MessageCampaignId = discountId,
            LanguageCode = "EN",
            LocationCode = "USA"
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedTierDiscountAsync(
        AppDbContext context,
        int discountId,
        double percent,
        double low,
        double high,
        byte statusId = 1,
        byte typeId = 4,
        byte methodId = 3,
        double years = 1)
    {
        context.CartDiscount.Add(new CartDiscount
        {
            CartDiscountId = discountId,
            CartDiscountTypeId = typeId,
            CartDiscountStatusId = statusId
        });
        context.CartDiscountItem.Add(new CartDiscountItem
        {
            CartDiscountItemId = discountId,
            CartDiscountId = discountId,
            CartDiscountMethodId = methodId,
            Discount = percent,
            LowRange = low,
            HighRange = high,
            ProductTypeId = 1,
            ProductLineId = 300,
            LicenseCategoryName = "SAEP",
            Years = years
        });
        await context.SaveChangesAsync();
    }

    private async Task SeedQaProductAsync(bool includePricing, string currencyCode = "USD")
    {
        await using var context = CreateContext();

        context.ProductType.Add(new ProductType
        {
            ProductTypeId = 1,
            ProductTypeDescription = "Standard"
        });
        context.LicenseKeycodeType.Add(new LicenseKeycodeType
        {
            LicenseKeycodeTypeId = 3,
            LicenseKeycodeTypeDescription = "Master"
        });
        context.ProductLine.Add(new ProductLine
        {
            ProductLineId = 300,
            ProductLineDescription = "Enterprise"
        });
        context.LicenseCategory.Add(new LicenseCategory
        {
            LicenseCategoryId = 220,
            LicenseCategoryName = "SAEP",
            LicenseCategoryDescription = "Endpoint Protection",
            BaseCapabilityId = 2
        });
        context.LicenseCategoryProductLine.Add(new LicenseCategoryProductLine
        {
            LicenseCategoryProductLineId = 1,
            LicenseCategoryId = 220,
            ProductLineId = 300,
            CartTypeId = 2,
            LanguageCode = "EN",
            LocationCode = "USA"
        });
        context.Product.Add(new Product
        {
            ProductId = SaepProductId,
            ProductDescription = "OpenText Core Endpoint Protection 30 Day Trial",
            ProductTypeId = 1,
            LicenseKeycodeTypeId = 3,
            UsesKeycode = 1
        });
        context.ProductLineProduct.Add(new ProductLineProduct
        {
            ProductId = SaepProductId,
            ProductLineId = 300
        });
        context.ProductLicenseCategory.Add(new ProductLicenseCategory
        {
            ProductId = SaepProductId,
            LicenseCategoryId = 220
        });
        context.ProductYears.Add(new ProductYears
        {
            ProductId = SaepProductId,
            Years = 0
        });
        context.ProductCapability.Add(new ProductCapability
        {
            ProductCapabilityId = 1,
            ProductId = SaepProductId,
            CapabilityId = 2,
            CapabilityTypeId = 2,
            CapabilityActivationDays = 30
        });

        if (includePricing)
        {
            context.Currency.Add(new Currency
            {
                CurrencyId = 1,
                CurrencyCode = currencyCode,
                CurrencyDescription = "US Dollar"
            });
            context.ProductPricing.Add(new ProductPricing
            {
                ProductPricingId = 1,
                ProductId = SaepProductId,
                LanguageCode = "EN",
                LocationCode = "USA",
                RetailPrice = 12.50m,
                CurrencyId = 1
            });
        }

        await context.SaveChangesAsync();
    }
}
