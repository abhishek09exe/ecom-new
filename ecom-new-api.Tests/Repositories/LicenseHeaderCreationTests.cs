using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.RegularExpressions;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Repositories.Trials;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ecom_new_api_tests.Repositories;

public sealed class LicenseHeaderCreationTests
{
    private static readonly DateTime Timestamp = new(2026, 10, 6, 9, 30, 0);

    private static LicenseCreationContext Context() => new(
        "SAEP", 17, 300, 2, 3, true, 1, Timestamp.Date, Timestamp.Date.AddDays(30),
        null, 30, 9, 7001, "GSMT", new[] { 41 }, Array.Empty<LicenseCreationModule>());

    private static string Json(string? companyType = "Business") => JsonSerializer.Serialize(new
    {
        customer = new
        {
            customer_email = "customer@company.example", first_name = "First", last_name = "Last",
            company_name = "Company", company_type_name = companyType, company_type_id = 14,
            phone_number = "555-0100", address_1 = "One", address_2 = "Two", city = "City",
            state = "CO", postal_code = "80000", country = "US", opt_in = 0
        },
        partner = new { partner_key = "unrelated-to-customer-partner-id", partner_account_code = "PARTNER-CODE" },
        sfdc = new { salesforce_account_id = "root-sfdc-value-not-customer" }
    });

    private static AppDbContext Database(CommandStub stub) => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true")
            .AddInterceptors(new NoConnection(), stub).Options);

    private static TrialRegistrationRepository Repository(AppDbContext db) => new(
        db, NullLogger<TrialRegistrationRepository>.Instance, new LicenseServiceStagingRepository(db));

    [Fact]
    public async Task InsertLicenseHeaderAsync_UsesCustomerOutputAndCapturesIdentityWithLegacyHeaderValues()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        var licenseId = await Repository(db).InsertLicenseHeaderAsync(Context(), "PREPARED-KEY", Json());

        Assert.Equal(4321, licenseId);
        var license = Assert.Single(db.ChangeTracker.Entries<License>()).Entity;
        Assert.Equal(987, license.CustomerId);
        Assert.Equal("PREPARED-KEY", license.Keycode);
        Assert.Equal(300, license.ProductLineId);
        Assert.Equal(2, license.LicenseTypeId);
        Assert.Equal(3, license.LicenseKeycodeTypeId);
        Assert.Equal(1, license.LicenseStatusId);
        Assert.Equal(7001, license.LicenseDistributionMethodId);
        Assert.Equal(Timestamp, license.InsertDate);
        Assert.Equal(license.InsertDate, license.ModifiedDate);
        Assert.Equal("license", license.InsertBy);
        Assert.Equal(license.InsertBy, license.ModifiedBy);
        Assert.Equal(30, license.MaxDailyActivations);
        Assert.Null(license.LicenseExpirationDate);
        Assert.Equal(1, stub.CustomerCalls);
        Assert.Equal(1, stub.InsertCalls);
        Assert.Equal(3, stub.Commands.Count);
        Assert.Contains("GETDATE()", stub.Commands[0]);
        Assert.Contains("usp_customer_insert_customer_service", stub.Commands[1]);
        Assert.Contains("INSERT INTO [license]", stub.Commands[2]);
        Assert.DoesNotContain(stub.Commands, command => command.Contains("MAX(", StringComparison.OrdinalIgnoreCase)
            || command.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase));
        Assert.All(db.ChangeTracker.Entries(), entry => Assert.IsType<License>(entry.Entity));
    }

    [Fact]
    public async Task InsertLicenseHeaderAsync_PreservesScalarSignatureAndDoesNotUseJsonOverload()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        await Repository(db).InsertLicenseHeaderAsync(Context(), "QA", Json());

        Assert.Equal(22, stub.CustomerParameters.Count);
        Assert.Equal("customer@company.example", stub.CustomerParameters["@customer_email"].Value);
        Assert.Equal("First", stub.CustomerParameters["@first_name"].Value);
        Assert.Equal("Last", stub.CustomerParameters["@last_name"].Value);
        Assert.Equal("Company", stub.CustomerParameters["@company_name"].Value);
        Assert.Equal("Business", stub.CustomerParameters["@company_type_name"].Value);
        Assert.Equal("CO", stub.CustomerParameters["@state"].Value);
        Assert.Equal(0, stub.CustomerParameters["@opt_in"].Value);
        Assert.Equal("license", stub.CustomerParameters["@transaction_user"].Value);
        foreach (var name in new[] { "@vendor_id", "@vendor_customer_code", "@partner_id",
            "@salesforce_account_id", "@salesforce_contact_id", "@customer_id_input", "@company_id_input" })
            Assert.Equal(DBNull.Value, stub.CustomerParameters[name].Value);
        Assert.Equal(ParameterDirection.Output, stub.CustomerParameters["@customer_id"].Direction);
        Assert.Equal(SqlDbType.VarChar, stub.CustomerParameters["@customer_email"].Type);
        Assert.Equal(SqlDbType.NVarChar, stub.CustomerParameters["@first_name"].Type);
        Assert.Equal(225, stub.CustomerParameters["@first_name"].Size);
        Assert.DoesNotContain("@input_json", stub.CustomerParameters.Keys);
        Assert.DoesNotContain("@company_type_id", stub.CustomerParameters.Keys);
    }

    [Fact]
    public async Task InsertLicenseHeaderAsync_ActivationLimitIsOmittedAndReturnedAsDatabaseDefault()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        await Repository(db).InsertLicenseHeaderAsync(Context(), "QA", Json());

        var insert = stub.Commands.Single(command => command.Contains("INSERT INTO [license]"));
        var insertColumns = insert[..insert.IndexOf("OUTPUT", StringComparison.Ordinal)];
        Assert.DoesNotContain("[max_daily_activations]", insertColumns);
        Assert.Contains("INSERTED.[max_daily_activations]", insert);
        var model = db.Model.FindEntityType(typeof(License))!;
        Assert.False(model.FindProperty(nameof(License.MaxDailyActivations))!.IsNullable);
        Assert.Equal(30, model.FindProperty(nameof(License.MaxDailyActivations))!.GetDefaultValue());
        Assert.Equal("datetime", model.FindProperty(nameof(License.InsertDate))!.GetColumnType());
        Assert.Equal("datetime", model.FindProperty(nameof(License.ModifiedDate))!.GetColumnType());
        Assert.False(model.FindProperty(nameof(License.InsertDate))!.IsNullable);
        Assert.Equal(30, db.ChangeTracker.Entries<License>().Single().Entity.MaxDailyActivations);
    }

    [Theory]
    [InlineData("duplicate key")]
    [InlineData("insert failed")]
    public async Task InsertLicenseHeaderAsync_InsertFailurePropagatesWithoutRetry(string failure)
    {
        var stub = new CommandStub { InsertFailure = failure };
        await using var db = Database(stub);
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;

        var error = await Assert.ThrowsAsync<DbUpdateException>(() =>
            Repository(db).InsertLicenseHeaderAsync(Context(), "QA", Json()));

        Assert.Contains(failure, error.InnerException!.Message);
        Assert.Equal(1, stub.InsertCalls);
        Assert.Equal(1, stub.CustomerCalls);
    }

    [Fact]
    public async Task InsertLicenseHeaderAsync_CustomerFailureDoesNotInsertHeader()
    {
        var stub = new CommandStub { CustomerFailure = true };
        await using var db = Database(stub);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Repository(db).InsertLicenseHeaderAsync(Context(), "QA", Json()));

        Assert.Equal(1, stub.CustomerCalls);
        Assert.Equal(0, stub.InsertCalls);
    }

    [Fact]
    public async Task InsertLicenseHeaderAsync_CompanyTypeUsesLegacyTwentyCharacterParseLimit()
    {
        var stub = new CommandStub();
        await using var db = Database(stub);
        db.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        await Repository(db).InsertLicenseHeaderAsync(Context(), "QA", Json("1234567890123456789012345"));
        Assert.Equal("12345678901234567890", stub.CustomerParameters["@company_type_name"].Value);
        Assert.Equal(50, stub.CustomerParameters["@company_type_name"].Size);
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
        public Dictionary<string, (object Value, SqlDbType Type, int Size, ParameterDirection Direction)> CustomerParameters { get; } = [];
        public int CustomerCalls { get; private set; }
        public int InsertCalls { get; private set; }
        public string? InsertFailure { get; init; }
        public bool CustomerFailure { get; init; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            Assert.Contains("usp_customer_insert_customer_service", command.CommandText);
            CustomerCalls++;
            foreach (SqlParameter parameter in command.Parameters)
                CustomerParameters.Add(parameter.ParameterName,
                    (parameter.Value, parameter.SqlDbType, parameter.Size, parameter.Direction));
            if (CustomerFailure)
                throw new InvalidOperationException("customer failed");
            command.Parameters["@customer_id"].Value = 987;
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(0));
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            var table = new DataTable();
            if (command.CommandText.Contains("GETDATE()"))
            {
                table.Columns.Add("Value", typeof(DateTime));
                table.Rows.Add(Timestamp);
            }
            else
            {
                Assert.Contains("INSERT INTO [license]", command.CommandText);
                InsertCalls++;
                if (InsertFailure is not null)
                    throw new InvalidOperationException(InsertFailure);
                var returned = Regex.Matches(command.CommandText, @"INSERTED\.\[([^\]]+)\]");
                Assert.NotEmpty(returned);
                foreach (Match match in returned)
                    table.Columns.Add(match.Groups[1].Value, typeof(int));
                table.Rows.Add(returned.Select(match => match.Groups[1].Value == "license_id" ? 4321 : 30)
                    .Cast<object>().ToArray());
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }
}