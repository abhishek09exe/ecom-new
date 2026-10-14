using System.Data.Common;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Repositories.Trials;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ecom_new_api_tests.Repositories;

public sealed class LicenseModuleCreationTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly Capture _capture = new();
    private readonly AppDbContext _db;
    private static readonly DateTime AuditTime = new(2026, 10, 6, 12, 0, 0);
    private const int ExistingLicenseId = 4321;
    private const byte TrialModuleType = 13;

    public LicenseModuleCreationTests()
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

    private static LicenseCreationContext Context(params LicenseCreationModule[] modules) => new(
        "SAEP", 17, 300, 2, 3, true, 1, AuditTime.Date, AuditTime.Date.AddDays(30),
        null, 30, 9, 7001, "GSMT", new[] { 41 }, modules);

    private static LicenseCreationModule Module(string category, byte moduleId, int seats = 7,
        DateTime? start = null, DateTime? end = null) =>
        new(category, 18, moduleId, category == "SDNS" ? "DNS" : category,
            TrialModuleType, seats, start ?? AuditTime.Date, end ?? AuditTime.Date.AddDays(30));

    private async Task SeedAsync()
    {
        _db.License.Add(new License
        {
            LicenseId = ExistingLicenseId, Keycode = "EXISTING-MASTER", ProductLineId = 300,
            LicenseTypeId = 2, LicenseKeycodeTypeId = 3, LicenseStatusId = 1,
            InsertDate = AuditTime, ModifiedDate = AuditTime, InsertBy = "license", ModifiedBy = "license"
        });
        _db.LicenseModuleType.Add(new LicenseModuleType
        {
            LicenseModuleTypeId = TrialModuleType, LicenseModuleTypeName = "trial"
        });
        _db.LicenseModule.AddRange(
            new LicenseModule { LicenseModuleId = 30, LicenseModuleCode = "DNS", LicenseModuleClassId = 3 },
            new LicenseModule { LicenseModuleId = 32, LicenseModuleCode = "SECA", LicenseModuleClassId = 3 },
            new LicenseModule { LicenseModuleId = 48, LicenseModuleCode = "OTEDR", LicenseModuleClassId = 3 },
            new LicenseModule { LicenseModuleId = 49, LicenseModuleCode = "OTMDR", LicenseModuleClassId = 3 });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        _capture.Commands.Clear();
    }

    [Fact]
    public async Task InsertModuleLicenseRecordsAsync_NoModulesDoesNotExecuteCommands()
    {
        await SeedAsync();
        var result = await Repository().InsertModuleLicenseRecordsAsync(
            ExistingLicenseId, Context(), AuditTime, "license");
        Assert.Empty(result);
        Assert.Empty(_capture.Commands);
        Assert.Empty(_db.ChangeTracker.Entries<LicenseModuleLicense>());
    }

    [Theory]
    [InlineData("SDNS", 30)]
    [InlineData("SECA", 32)]
    [InlineData("OTEDR", 48)]
    [InlineData("OTMDR", 49)]
    public async Task InsertModuleLicenseRecordsAsync_UsesResolvedMappingAndCapturesIdentity(string category, int moduleId)
    {
        await SeedAsync();
        var result = await Repository().InsertModuleLicenseRecordsAsync(
            ExistingLicenseId, Context(Module(category, (byte)moduleId)), AuditTime, "license");
        var id = Assert.Single(result);
        Assert.True(id > 0);
        var row = await _db.LicenseModuleLicense.AsNoTracking().SingleAsync();
        Assert.Equal(id, row.LicenseModuleLicenseId);
        Assert.Equal(ExistingLicenseId, row.LicenseId);
        Assert.Equal((byte)moduleId, row.LicenseModuleId);
        Assert.Equal(TrialModuleType, row.LicenseModuleTypeId);
        Assert.Equal(7, row.ModuleSeats);
        Assert.Equal(AuditTime.Date, row.StartDate);
        Assert.Equal(AuditTime.Date.AddDays(30), row.EndDate);
        Assert.Equal(1, row.LicenseStatusId);
        Assert.Equal(AuditTime, row.LastModifiedDate);
        Assert.Equal("license", row.LastModifiedBy);
    }

    [Fact]
    public async Task InsertModuleLicenseRecordsAsync_MultipleModulesPreserveTheirOwnSeatsAndDates()
    {
        await SeedAsync();
        var first = Module("SDNS", 30, 7, AuditTime.Date.AddDays(1), AuditTime.Date.AddDays(31));
        var second = Module("SECA", 32, 19, AuditTime.Date.AddDays(3), AuditTime.Date.AddDays(33));
        var result = await Repository().InsertModuleLicenseRecordsAsync(
            ExistingLicenseId, Context(first, second), AuditTime, "license");
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Distinct().Count());
        var rows = await _db.LicenseModuleLicense.AsNoTracking().OrderBy(row => row.LicenseModuleId).ToListAsync();
        Assert.Equal(first.Seats, rows[0].ModuleSeats);
        Assert.Equal(first.StartDate, rows[0].StartDate);
        Assert.Equal(first.ExpirationDate, rows[0].EndDate);
        Assert.Equal(second.Seats, rows[1].ModuleSeats);
        Assert.Equal(second.StartDate, rows[1].StartDate);
        Assert.Equal(second.ExpirationDate, rows[1].EndDate);
    }

    [Fact]
    public async Task InsertModuleLicenseRecordsAsync_NullModuleDatesRemainNull()
    {
        await SeedAsync();
        var module = Module("SDNS", 30) with { StartDate = null, ExpirationDate = null };
        await Repository().InsertModuleLicenseRecordsAsync(ExistingLicenseId, Context(module), AuditTime, "license");
        var row = await _db.LicenseModuleLicense.AsNoTracking().SingleAsync();
        Assert.Null(row.StartDate);
        Assert.Null(row.EndDate);
    }

    [Fact]
    public async Task InsertModuleLicenseRecordsAsync_RepeatedMappingProducesRepeatedRowsWithoutDeduplication()
    {
        await SeedAsync();
        var repeated = Module("SDNS", 30);
        var result = await Repository().InsertModuleLicenseRecordsAsync(
            ExistingLicenseId, Context(repeated, repeated), AuditTime, "license");
        Assert.Equal(2, result.Count);
        Assert.Equal(2, result.Distinct().Count());
        Assert.Equal(2, await _db.LicenseModuleLicense.CountAsync());
    }

    [Theory]
    [InlineData("license")]
    [InlineData("module")]
    [InlineData("type")]
    public async Task InsertModuleLicenseRecordsAsync_ForeignKeyFailurePropagatesWithoutRetry(string missing)
    {
        await SeedAsync();
        var module = Module("SDNS", 30);
        var licenseId = ExistingLicenseId;
        if (missing == "license") licenseId = 9999;
        if (missing == "module") module = module with { LicenseModuleId = 250 };
        if (missing == "type") module = module with { LicenseModuleTypeId = 250 };

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            Repository().InsertModuleLicenseRecordsAsync(licenseId, Context(module), AuditTime, "license"));
        Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Contains("FOREIGN KEY", exception.InnerException!.Message);
        Assert.Single(_capture.Commands, command => command.Contains("INSERT INTO \"license_module_license\""));
    }

    [Fact]
    public async Task InsertModuleLicenseRecordsAsync_WritesOnlyModuleAssignmentsWithoutLookupQueries()
    {
        await SeedAsync();
        await Repository().InsertModuleLicenseRecordsAsync(
            ExistingLicenseId, Context(Module("SDNS", 30), Module("OTMDR", 49)), AuditTime, "license");
        Assert.Equal(2, _capture.Commands.Count);
        Assert.All(_capture.Commands, command => Assert.StartsWith("INSERT INTO \"license_module_license\"", command.TrimStart()));
        Assert.All(_db.ChangeTracker.Entries(), entry => Assert.IsType<LicenseModuleLicense>(entry.Entity));
    }

    [Fact]
    public async Task InsertModuleLicenseRecordsAsync_RejectsUnrelatedPendingWrites()
    {
        await SeedAsync();
        _db.LicenseModule.Add(new LicenseModule { LicenseModuleId = 250, LicenseModuleCode = "PENDING", LicenseModuleClassId = 3 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => Repository().InsertModuleLicenseRecordsAsync(
            ExistingLicenseId, Context(Module("SDNS", 30)), AuditTime, "license"));
        Assert.Empty(_capture.Commands);
    }

    [Fact]
    public void Mapping_PreservesSqlTypesIdentityForeignKeysAndHasNoAssignmentUniqueness()
    {
        var model = _db.Model.FindEntityType(typeof(LicenseModuleLicense))!;
        Assert.Equal(nameof(LicenseModuleLicense.LicenseModuleLicenseId), Assert.Single(model.FindPrimaryKey()!.Properties).Name);
        Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd,
            model.FindProperty(nameof(LicenseModuleLicense.LicenseModuleLicenseId))!.ValueGenerated);
        Assert.Equal("datetime", model.FindProperty(nameof(LicenseModuleLicense.StartDate))!.GetColumnType());
        Assert.True(model.FindProperty(nameof(LicenseModuleLicense.StartDate))!.IsNullable);
        Assert.True(model.FindProperty(nameof(LicenseModuleLicense.EndDate))!.IsNullable);
        Assert.True(model.FindProperty(nameof(LicenseModuleLicense.LicenseStatusId))!.IsNullable);
        Assert.False(model.FindProperty(nameof(LicenseModuleLicense.LastModifiedDate))!.IsNullable);
        Assert.False(model.FindProperty(nameof(LicenseModuleLicense.LastModifiedBy))!.IsNullable);
        Assert.Equal(200, model.FindProperty(nameof(LicenseModuleLicense.LastModifiedBy))!.GetMaxLength());
        Assert.False(model.FindProperty(nameof(LicenseModuleLicense.LastModifiedBy))!.IsUnicode());
        Assert.Equal(3, model.GetForeignKeys().Count());
        Assert.DoesNotContain(model.GetIndexes(), index => index.IsUnique);
    }

    private sealed class Capture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}