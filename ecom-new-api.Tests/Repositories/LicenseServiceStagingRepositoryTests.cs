using System.Data.Common;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Repositories.Trials;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace ecom_new_api_tests.Repositories;

public sealed class LicenseServiceStagingRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly CommandCapture _capture = new();
    private readonly AppDbContext _db;

    public LicenseServiceStagingRepositoryTests()
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

    [Fact]
    public async Task StageLicenseCreateAsync_PersistsOneHeaderAndExactJsonWithGeneratedIdentity()
    {
        await SeedAsync();
        const string json = "{ \"keycode\":\"QA-KEY\", \"ecom_create_license\":1, \"customer\":{\"company_name\":\"A & B\"} }";
        _capture.Commands.Clear();

        var serviceId = await new LicenseServiceStagingRepository(_db).StageLicenseCreateAsync(json);

        var header = await _db.LicenseService.SingleAsync();
        var payload = await _db.LicenseServiceJson.SingleAsync();
        Assert.True(serviceId > 0);
        Assert.Equal(serviceId, header.LicenseServiceId);
        Assert.Equal(serviceId, payload.LicenseServiceId);
        Assert.Equal((byte)20, header.LicenseServiceTypeId);
        Assert.Equal((byte)7, header.LicenseServiceStatusId);
        Assert.Equal(json, payload.ServiceJson);
        Assert.NotEqual(default, header.InsertDate);
        Assert.NotNull(header.ProcessDate);
        Assert.Equal((byte)0, header.UpdateAttempts);
        Assert.Single(_capture.Commands, sql => sql.Contains("INSERT INTO \"license_service\""));
        Assert.Single(_capture.Commands, sql => sql.Contains("INSERT INTO \"license_service_json\""));
        Assert.DoesNotContain(_capture.Commands, sql => sql.Contains("license_service_value", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(_capture.Commands, sql => sql.Contains("task_table", StringComparison.OrdinalIgnoreCase)
            || sql.Contains("usp_queue_task", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task StageLicenseCreateAsync_JsonInsertFailureRollsBackHeader()
    {
        await SeedAsync();
        _capture.FailLicenseServiceJsonInsert = true;

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            new LicenseServiceStagingRepository(_db).StageLicenseCreateAsync("{\"keycode\":\"QA\"}"));

        _capture.FailLicenseServiceJsonInsert = false;
        Assert.Empty(await _db.LicenseService.AsNoTracking().ToListAsync());
        Assert.Empty(await _db.LicenseServiceJson.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData(false, true, "synchronous", 20)]
    [InlineData(true, false, "synchronous", 20)]
    [InlineData(true, true, "asynchronous", 20)]
    [InlineData(true, true, "synchronous", 50)]
    public async Task StageLicenseCreateAsync_InvalidConfiguration_WritesNothing(
        bool includeType, bool includeStatus, string processType, int typeId)
    {
        await SeedAsync(includeType, includeStatus, processType, (byte)typeId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LicenseServiceStagingRepository(_db).StageLicenseCreateAsync("{\"keycode\":\"QA\"}"));

        Assert.Empty(await _db.LicenseService.ToListAsync());
        Assert.Empty(await _db.LicenseServiceJson.ToListAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    public async Task StageLicenseCreateAsync_EmptyPayload_WritesNothing(string json)
    {
        await SeedAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new LicenseServiceStagingRepository(_db).StageLicenseCreateAsync(json));
        Assert.Empty(await _db.LicenseService.ToListAsync());
    }

    [Fact]
    public async Task Mapping_JsonHasNoActiveHeaderForeignKey()
    {
        await SeedAsync();
        var id = await new LicenseServiceStagingRepository(_db).StageLicenseCreateAsync("{\"keycode\":\"QA\"}");
        await _db.LicenseService.Where(service => service.LicenseServiceId == id).ExecuteDeleteAsync();
        Assert.Equal(id, (await _db.LicenseServiceJson.SingleAsync()).LicenseServiceId);
        Assert.Empty(_db.Model.FindEntityType(typeof(LicenseServiceJson))!.GetForeignKeys());
    }

    private async Task SeedAsync(bool includeType = true, bool includeStatus = true,
        string processType = "synchronous", byte typeId = 20)
    {
        if (includeType)
            _db.LicenseServiceType.Add(new LicenseServiceType
            {
                LicenseServiceTypeId = typeId,
                LicenseServiceTypeName = "license_create",
                ProcessType = processType
            });
        if (includeStatus)
            _db.LicenseServiceStatus.Add(new LicenseServiceStatus
            {
                LicenseServiceStatusId = 7,
                LicenseServiceStatusName = "pending"
            });
        await _db.SaveChangesAsync();
    }

    private sealed class CommandCapture : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];
        public bool FailLicenseServiceJsonInsert { get; set; }

        private void ThrowOnLicenseServiceJsonInsert(DbCommand command)
        {
            if (FailLicenseServiceJsonInsert &&
                command.CommandText.Contains("INSERT INTO \"license_service_json\"", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Injected license-service JSON insert failure.");
            }
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ThrowOnLicenseServiceJsonInsert(command);
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ThrowOnLicenseServiceJsonInsert(command);
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}