using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Repositories.Trials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ecom_new_api_tests.Repositories;

public sealed class LicensePrimaryCreationTests
{
    private const int LicenseId = 4321;
    private static readonly DateTime Start = new(2026, 10, 6, 23, 45, 0);
    private static readonly DateTime End = new(2026, 11, 5, 0, 15, 0);
    private static readonly DateTime AuditTime = new(2026, 10, 6, 12, 0, 0);
    private static readonly Guid GeneratedKey = Guid.Parse("435bd6c8-0e04-4130-a8fb-bfc9bdab272c");

    private static LicenseCreationContext Context() => new(
        "SAEP", 17, 300, 2, 3, true, 1, Start, End, null, 999,
        9, 7001, "GSMT", new[] { 41, 42 },
        new[] { new LicenseCreationModule("SDNS", 18, 55, "DNS", 13, 7, Start, End) });

    private static AppDbContext Database(CommandStub stub)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true",
                options => options.MaxBatchSize(1))
            .AddInterceptors(new NoConnection(), stub).Options);
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        return db;
    }

    private static TrialRegistrationRepository Repository(AppDbContext db) => new(
        db, NullLogger<TrialRegistrationRepository>.Instance, new LicenseServiceStagingRepository(db));

    [Fact]
    public async Task InsertPrimaryLicenseRecordsAsync_CreatesOnlyPrimaryRecordsInExactOrderAndCapturesGeneratedValues()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);

        var result = await Repository(db).InsertPrimaryLicenseRecordsAsync(
            LicenseId, Context(), AuditTime, "license");

        Assert.Equal(new[] { "license_category_license", "license_capability", "license_capability",
            "license_seat" }, stub.Tables);
        var category = Assert.Single(db.ChangeTracker.Entries<LicenseCategoryLicense>()).Entity;
        Assert.Equal(LicenseId, category.LicenseId);
        Assert.Equal((byte)17, category.LicenseCategoryId);
        Assert.Equal(Start, category.StartDate);
        Assert.Equal(End, category.EndDate);
        Assert.Equal(AuditTime, category.LastModifiedDate);
        Assert.Equal("license", category.LastModifiedBy);
        Assert.True(category.LicenseCategoryLicenseId > 0);
        Assert.Equal(category.LicenseCategoryLicenseId, result.LicenseCategoryLicenseId);

        var capabilities = db.ChangeTracker.Entries<LicenseCapability>().Select(entry => entry.Entity).ToList();
        Assert.Equal(new[] { 41, 42 }, capabilities.Select(capability => capability.CapabilityId).OrderBy(id => id));
        Assert.All(capabilities, capability =>
        {
            Assert.Equal(LicenseId, capability.LicenseId);
            Assert.Equal(9, capability.CapabilityTypeId);
            Assert.Equal(30, capability.CapabilityActivationDays);
            Assert.Equal(Start, capability.CapabilityActivationDate);
            Assert.Equal(End, capability.CapabilityExpirationDate);
            Assert.Equal(AuditTime, capability.InsertDate);
            Assert.Equal(AuditTime, capability.ModifiedDate);
            Assert.Equal("license", capability.InsertBy);
            Assert.Equal("license", capability.ModifiedBy);
            Assert.Contains(capability.LicenseCapabilityId, result.LicenseCapabilityIds);
        });
        var seat = Assert.Single(db.ChangeTracker.Entries<LicenseSeat>()).Entity;
        Assert.Equal(1, seat.LicenseSeats);
        Assert.Equal(0, seat.SeatsUsed);
        Assert.Equal(Start, seat.StartDate);
        Assert.Equal(End, seat.EndDate);
        Assert.Equal(AuditTime, seat.InsertDate);
        Assert.Equal(AuditTime, seat.ModifiedDate);
        Assert.Equal("license", seat.InsertBy);
        Assert.Equal("license", seat.ModifiedBy);
        Assert.Equal(seat.LicenseSeatId, result.LicenseSeatId);
        Assert.True(result.LicenseSeatId > 0);
        Assert.Empty(db.ChangeTracker.Entries<LicenseKey>());
        Assert.Empty(db.ChangeTracker.Entries<LicenseFeatureLicense>());
    }

    [Fact]
    public async Task InsertPrimaryLicenseRecordsAsync_OmitsDatabaseGeneratedDefaultsFromInsertColumns()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        await Repository(db).InsertPrimaryLicenseRecordsAsync(LicenseId, Context(), AuditTime, "license");

        string Columns(string table) => stub.Commands[stub.Tables.IndexOf(table)]
            .Split("OUTPUT", StringSplitOptions.None)[0];
        Assert.DoesNotContain("[seats_used]", Columns("license_seat"));
        Assert.DoesNotContain("license_key", stub.Tables);
        Assert.DoesNotContain("license_feature_license", stub.Tables);
    }

    [Theory]
    [InlineData("{}", null)]
    [InlineData("{\"sfdc\":{\"salesforce_license_id\":null}}", null)]
    [InlineData("{\"sfdc\":{\"salesforce_license_id\":\"123456789012345678\"}}", "123456789012345678")]
    [InlineData("{\"sfdc\":{\"salesforce_license_id\":\"SFDC\"}}", "SFDC              ")]
    public async Task InsertLicenseKeyAsync_UsesSuppliedOrNullSalesforceId(string json, string? expected)
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        await Repository(db).InsertLicenseKeyAsync(LicenseId, json);
        Assert.Equal(expected, db.ChangeTracker.Entries<LicenseKey>().Single().Entity.SalesforceLicenseId);
    }

    [Fact]
    public async Task InsertLicenseKeyAsync_CapturesDatabaseGeneratedKey()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        var key = await Repository(db).InsertLicenseKeyAsync(LicenseId, "{}");
        var entity = Assert.Single(db.ChangeTracker.Entries<LicenseKey>()).Entity;
        Assert.Equal(LicenseId, entity.LicenseId);
        Assert.Equal(GeneratedKey, entity.Key);
        Assert.Equal(GeneratedKey, key);
        var insert = stub.Commands.Single();
        Assert.DoesNotContain("[license_key]", insert.Split("OUTPUT", StringSplitOptions.None)[0]
            .Replace("INSERT INTO [license_key]", string.Empty));
        Assert.Contains("[license_id]", insert);
        var model = db.Model.FindEntityType(typeof(LicenseKey))!;
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never,
            model.FindProperty(nameof(LicenseKey.LicenseId))!.ValueGenerated);
        Assert.Equal("NEWID()", model.FindProperty(nameof(LicenseKey.Key))!.GetDefaultValueSql());
    }

    [Fact]
    public async Task InsertLicenseFeatureLicenseAsync_TypeThreeUsesDatabaseGeneratedDefaults()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        var id = await Repository(db).InsertLicenseFeatureLicenseAsync(LicenseId, Context());
        var feature = Assert.Single(db.ChangeTracker.Entries<LicenseFeatureLicense>()).Entity;
        Assert.Equal(LicenseId, feature.LicenseId);
        Assert.Equal(8, feature.LicenseFeatureId);
        Assert.Equal(AuditTime.AddHours(1), feature.InsertDate);
        Assert.Equal("sql-login", feature.InsertBy);
        Assert.Null(feature.LicenseFeatureValue);
        Assert.Equal(feature.LicenseFeatureLicenseId, id);
        Assert.Equal(new[] { "license_feature_license" }, stub.Tables);
        Assert.DoesNotContain("[insert_date]", stub.Commands.Single().Split("OUTPUT", StringSplitOptions.None)[0]);
        Assert.DoesNotContain("[insert_by]", stub.Commands.Single().Split("OUTPUT", StringSplitOptions.None)[0]);
        var model = db.Model.FindEntityType(typeof(LicenseFeatureLicense))!;
        Assert.Equal("GETDATE()", model.FindProperty(nameof(LicenseFeatureLicense.InsertDate))!.GetDefaultValueSql());
        Assert.Equal("SUSER_SNAME()", model.FindProperty(nameof(LicenseFeatureLicense.InsertBy))!.GetDefaultValueSql());
    }

    [Fact]
    public async Task InsertLicenseFeatureLicenseAsync_NonTypeThreeSkipsFeature()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        var context = Context() with { LicenseKeycodeTypeId = 1, LicenseTypeId = 4 };
        var id = await Repository(db).InsertLicenseFeatureLicenseAsync(LicenseId, context);
        Assert.Null(id);
        Assert.DoesNotContain("license_feature_license", stub.Tables);
        Assert.Empty(db.ChangeTracker.Entries<LicenseFeatureLicense>());
    }

    [Fact]
    public async Task InsertLicenseRecordsInLegacyOrderAsync_PreservesCrossSliceWriteOrder()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        var result = await Repository(db).InsertLicenseRecordsInLegacyOrderAsync(
            LicenseId, Context(), "{}", AuditTime, "license");

        Assert.Equal(new[]
        {
            "license_category_license", "license_capability", "license_capability", "license_seat",
            "license_module_license", "license_key", "license_feature_license"
        }, stub.Tables);
        Assert.True(result.LicenseCategoryLicenseId > 0);
        Assert.Equal(2, result.LicenseCapabilityIds.Count);
        Assert.True(result.LicenseSeatId > 0);
        Assert.Single(result.LicenseModuleLicenseIds);
        Assert.Equal(GeneratedKey, result.LicenseKey);
        Assert.True(result.LicenseFeatureLicenseId > 0);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("FK")]
    public async Task InsertPrimaryLicenseRecordsAsync_ConstraintFailurePropagatesWithoutRetry(string failure)
    {
        var stub = new CommandStub { FailOnTable = "license_category_license", Failure = failure };
        await using var db = Database(stub);
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            Repository(db).InsertPrimaryLicenseRecordsAsync(LicenseId, Context(), AuditTime, "license"));
        Assert.Contains(failure, exception.InnerException!.Message);
        Assert.Single(stub.Tables);
    }

    [Fact]
    public async Task InsertPrimaryLicenseRecordsAsync_DoesNotWriteKeyOrFeature()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        await Repository(db).InsertPrimaryLicenseRecordsAsync(LicenseId, Context(), AuditTime, "license");
        Assert.Contains("license_category_license", stub.Tables);
        Assert.DoesNotContain("license_key", stub.Tables);
        Assert.DoesNotContain("license_feature_license", stub.Tables);
    }

    [Fact]
    public async Task InsertPrimaryLicenseRecordsAsync_HierarchyTwoCreatesNoPrimaryRowsOrOtherTableWrites()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        await Repository(db).InsertPrimaryLicenseRecordsAsync(LicenseId, Context(), AuditTime, "license");
        Assert.Single(db.ChangeTracker.Entries<LicenseCategoryLicense>());
        Assert.Equal(2, db.ChangeTracker.Entries<LicenseCapability>().Count());
        Assert.Single(db.ChangeTracker.Entries<LicenseSeat>());
        var allowed = new[] { "license_category_license", "license_capability", "license_seat" };
        Assert.All(stub.Tables, table => Assert.Contains(table, allowed));
        Assert.Equal(stub.Tables.Count, stub.Commands.Count);
    }

    private sealed class NoConnection : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class CommandStub : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public List<string> Tables { get; } = [];
        public string? FailOnTable { get; init; }
        public string Failure { get; init; } = string.Empty;
        private int _identity = 100;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            var tableName = Regex.Match(command.CommandText, @"INSERT INTO \[([^\]]+)\]").Groups[1].Value;
            Assert.NotEmpty(tableName);
            Tables.Add(tableName);
            if (tableName == FailOnTable)
                throw new InvalidOperationException(Failure);
            var table = new DataTable();
            var values = new List<object>();
            var returned = Regex.Matches(command.CommandText, @"INSERTED\.\[([^\]]+)\]");
            Assert.NotEmpty(returned);
            foreach (Match match in returned)
            {
                var column = match.Groups[1].Value;
                var type = column switch
                {
                    "license_key" => typeof(Guid),
                    "insert_date" => typeof(DateTime),
                    "insert_by" => typeof(string),
                    _ => typeof(int)
                };
                table.Columns.Add(column, type);
                values.Add(column switch
                {
                    "license_key" => GeneratedKey,
                    "insert_date" => AuditTime.AddHours(1),
                    "insert_by" => "sql-login",
                    "seats_used" => 0,
                    _ => ++_identity
                });
            }
            table.Rows.Add(values.ToArray());
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}