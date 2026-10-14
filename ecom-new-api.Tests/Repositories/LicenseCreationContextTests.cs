using System.Data;
using System.Data.Common;
using System.Text.Json;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Repositories.Trials;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ecom_new_api_tests.Repositories;

public sealed class LicenseCreationContextTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly QueryCapture _capture = new();
    private readonly AppDbContext _db;
    private static readonly DateTime Start = new(2026, 10, 6, 15, 30, 0);
    private static readonly DateTime End = Start.AddDays(30);

    public LicenseCreationContextTests()
    {
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).AddInterceptors(_capture).Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private TrialRegistrationRepository Repository() => new(_db,
        NullLogger<TrialRegistrationRepository>.Instance, new LicenseServiceStagingRepository(_db));

    [Fact]
    public async Task Resolve_Saep_UsesMasterTypeAndPreservesExplicitDates()
    {
        await SeedAsync();
        var context = await Repository().ResolveLicenseCreationContextAsync(Payload());
        Assert.Equal("SAEP", context.PrimaryCategoryName);
        Assert.Equal(300, context.ProductLineId);
        Assert.Equal(2, context.LicenseTypeId);
        Assert.Equal(3, context.LicenseKeycodeTypeId);
        Assert.True(context.IsTrial);
        Assert.Equal(1, context.Seats);
        Assert.Equal(Start, context.StartDate);
        Assert.Equal(End, context.ExpirationDate);
        Assert.Equal(30, context.CapabilityActivationDays);
        Assert.Null(context.LegacyTrialDays);
    }

    [Fact]
    public async Task Resolve_UsesAllCapabilityMappingsAndDatabaseDrivenIdsWithoutProducts()
    {
        await SeedAsync();
        var context = await Repository().ResolveLicenseCreationContextAsync(Payload());
        Assert.Equal((byte)17, context.LicenseCategoryId);
        Assert.Equal(9, context.TrialCapabilityTypeId);
        Assert.Equal(new[] { 41, 42 }, context.CapabilityIds.OrderBy(id => id));
        Assert.Equal(7001, context.LicenseDistributionMethodId);
        Assert.Equal("GSMT", context.LicenseDistributionMethodCode);
        Assert.Empty(await _db.Product.ToListAsync());
        Assert.Empty(await _db.License.ToListAsync());
    }

    [Fact]
    public async Task Resolve_UsesEnUsaEvenWhenJsonHasOtherLocaleAndCartTypeIsNotTwo()
    {
        await SeedAsync();
        _db.LicenseCategoryProductLine.Add(new LicenseCategoryProductLine
        {
            LicenseCategoryProductLineId = 2, LicenseCategoryId = 17,
            ProductLineId = 55, CartTypeId = 2, LanguageCode = "FR", LocationCode = "CAN"
        });
        await _db.SaveChangesAsync();
        var context = await Repository().ResolveLicenseCreationContextAsync(Payload());
        Assert.Equal(300, context.ProductLineId);
    }

    [Fact]
    public async Task Resolve_CategorySpecificDistributionPrecedesGenericMapping()
    {
        await SeedAsync();
        _db.LicenseDistributionMethod.Add(new LicenseDistributionMethod
        {
            LicenseDistributionMethodId = 7002, LicenseDistributionMethodCode = "SPEC"
        });
        _db.ProductLineLicenseDistributionMethod.Add(new ProductLineLicenseDistributionMethod
        {
            ProductLineLicenseDistributionMethodId = 2, ProductLineId = 300,
            LicenseKeycodeTypeId = 3, Trial = 1, LicenseCategoryId = 17,
            LicenseDistributionMethodId = 7002
        });
        await _db.SaveChangesAsync();
        var context = await Repository().ResolveLicenseCreationContextAsync(Payload());
        Assert.Equal(7002, context.LicenseDistributionMethodId);
    }

    [Fact]
    public async Task Resolve_ValidExplicitCodePrecedesDimensionFallback()
    {
        await SeedAsync();
        _db.LicenseDistributionMethod.Add(new LicenseDistributionMethod
        {
            LicenseDistributionMethodId = 7003, LicenseDistributionMethodCode = "EXPL"
        });
        await _db.SaveChangesAsync();
        var context = await Repository().ResolveLicenseCreationContextAsync(Payload("EXPL"));
        Assert.Equal(7003, context.LicenseDistributionMethodId);
        Assert.Equal("EXPL", context.LicenseDistributionMethodCode);
    }

    [Fact]
    public async Task Resolve_UnknownExplicitCodeUsesDimensionFallback()
    {
        await SeedAsync();
        var context = await Repository().ResolveLicenseCreationContextAsync(Payload("NONE"));
        Assert.Equal(7001, context.LicenseDistributionMethodId);
    }

    [Fact]
    public async Task Resolve_ModuleConfigurationUsesClassThreeAndDatabaseTrialType()
    {
        await SeedAsync();
        _db.LicenseCategory.Add(new LicenseCategory { LicenseCategoryId = 18, LicenseCategoryName = "SDNS" });
        _db.LicenseModule.AddRange(
            new LicenseModule { LicenseModuleId = 55, LicenseModuleCode = "DNS", LicenseModuleClassId = 3 },
            new LicenseModule { LicenseModuleId = 56, LicenseModuleCode = "SITE", LicenseModuleClassId = 1 });
        _db.LicenseModuleCategory.AddRange(
            new LicenseModuleCategory { LicenseModuleCategoryId = 1, LicenseCategoryId = 18, LicenseModuleId = 55 },
            new LicenseModuleCategory { LicenseModuleCategoryId = 2, LicenseCategoryId = 18, LicenseModuleId = 56 });
        _db.LicenseModuleType.Add(new LicenseModuleType { LicenseModuleTypeId = 13, LicenseModuleTypeName = "trial" });
        await _db.SaveChangesAsync();
        var context = await Repository().ResolveLicenseCreationContextAsync(Payload(includeModule: true));
        var module = Assert.Single(context.Modules);
        Assert.Equal((byte)55, module.LicenseModuleId);
        Assert.Equal((byte)13, module.LicenseModuleTypeId);
        Assert.Equal("DNS", module.LicenseModuleCode);
        Assert.Equal(7, module.Seats);
        Assert.Equal(Start, module.StartDate);
        Assert.Equal(End, module.ExpirationDate);
    }

    [Theory]
    [InlineData("category")]
    [InlineData("line")]
    [InlineData("capability")]
    [InlineData("type")]
    [InlineData("distribution")]
    public async Task Resolve_MissingConfigurationFails(string missing)
    {
        await SeedAsync();
        switch (missing)
        {
            case "category": await _db.LicenseCategoryProductLine.ExecuteDeleteAsync(); await _db.LicenseCategory.ExecuteDeleteAsync(); break;
            case "line": await _db.LicenseCategoryProductLine.ExecuteDeleteAsync(); break;
            case "capability": await _db.LicenseCategoryCapability.ExecuteDeleteAsync(); break;
            case "type": await _db.CapabilityType.ExecuteDeleteAsync(); break;
            case "distribution": await _db.ProductLineLicenseDistributionMethod.ExecuteDeleteAsync(); break;
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => Repository().ResolveLicenseCreationContextAsync(Payload()));
    }

    [Theory]
    [InlineData("line")]
    [InlineData("type")]
    [InlineData("distribution")]
    public async Task Resolve_AmbiguousConfigurationFails(string ambiguous)
    {
        await SeedAsync();
        switch (ambiguous)
        {
            case "line":
                _db.LicenseCategoryProductLine.Add(new LicenseCategoryProductLine
                {
                    LicenseCategoryProductLineId = 2, LicenseCategoryId = 17, ProductLineId = 300,
                    CartTypeId = 2, LanguageCode = "EN", LocationCode = "USA"
                });
                break;
            case "type":
                _db.CapabilityType.Add(new CapabilityType { CapabilityTypeId = 10, CapabilityTypeDescription = "trial" });
                break;
            case "distribution":
                _db.ProductLineLicenseDistributionMethod.Add(new ProductLineLicenseDistributionMethod
                {
                    ProductLineLicenseDistributionMethodId = 2, ProductLineId = 300,
                    LicenseKeycodeTypeId = 3, Trial = 1, LicenseDistributionMethodId = 7001
                });
                break;
        }
        await _db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Repository().ResolveLicenseCreationContextAsync(Payload()));
    }

    [Fact]
    public async Task Resolve_PerformsOnlySelectsAndNeverCallsCreationProcedure()
    {
        await SeedAsync();
        _db.ChangeTracker.Clear();
        _capture.Commands.Clear();
        await Repository().ResolveLicenseCreationContextAsync(Payload());
        Assert.NotEmpty(_capture.Commands);
        Assert.All(_capture.Commands, command =>
        {
            Assert.Equal(CommandType.Text, command.Type);
            Assert.StartsWith("SELECT", command.Text.TrimStart());
        });
        Assert.False(_db.ChangeTracker.HasChanges());
    }

    private async Task SeedAsync()
    {
        _db.LicenseCategory.Add(new LicenseCategory
        {
            LicenseCategoryId = 17, LicenseCategoryName = "SAEP", BaseCapabilityId = 99
        });
        _db.LicenseCategoryProductLine.Add(new LicenseCategoryProductLine
        {
            LicenseCategoryProductLineId = 1, LicenseCategoryId = 17, ProductLineId = 300,
            CartTypeId = 1, LanguageCode = "EN", LocationCode = "USA"
        });
        _db.LicenseCategoryCapability.AddRange(
            new LicenseCategoryCapability { LicenseCategoryCapabilityId = 1, LicenseCategoryId = 17, CapabilityId = 41 },
            new LicenseCategoryCapability { LicenseCategoryCapabilityId = 2, LicenseCategoryId = 17, CapabilityId = 42 });
        _db.CapabilityType.Add(new CapabilityType { CapabilityTypeId = 9, CapabilityTypeDescription = "trial" });
        _db.LicenseDistributionMethod.Add(new LicenseDistributionMethod
        {
            LicenseDistributionMethodId = 7001, LicenseDistributionMethodCode = "GSMT"
        });
        _db.ProductLineLicenseDistributionMethod.Add(new ProductLineLicenseDistributionMethod
        {
            ProductLineLicenseDistributionMethodId = 1, ProductLineId = 300,
            LicenseKeycodeTypeId = 3, Trial = 1, LicenseDistributionMethodId = 7001
        });
        await _db.SaveChangesAsync();
    }

    private static string Payload(string distributionCode = "", bool includeModule = false)
    {
        var categories = new List<object>
        {
            new { license_category_name = "SAEP", item_hierarchy_id = 1, category_type_name = "trial",
                license_seats = 1, days = 30, start_date = Start, expiration_date = End }
        };
        if (includeModule)
            categories.Add(new { license_category_name = "SDNS", item_hierarchy_id = 2, category_type_name = "trial",
                license_seats = 7, days = 30, start_date = Start, expiration_date = End });
        return JsonSerializer.Serialize(new
        {
            license_keycode_type_id = 3, license_distribution_method_code = distributionCode,
            language_code = "FR", location_code = "CAN", license_category = categories
        });
    }

    private sealed class QueryCapture : DbCommandInterceptor
    {
        public List<(CommandType Type, string Text)> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add((command.CommandType, command.CommandText));
            return ValueTask.FromResult(result);
        }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add((command.CommandType, command.CommandText));
            return ValueTask.FromResult(result);
        }
    }
}