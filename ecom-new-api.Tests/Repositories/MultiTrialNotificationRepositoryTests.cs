using ecom_new_api.Data;
using ecom_new_api.Repositories.Trials;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ecom_new_api_tests.Repositories;

public sealed class MultiTrialNotificationRepositoryTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("not-json", false)]
    [InlineData("null", false)]
    [InlineData("[]", false)]
    [InlineData("{}", false)]
    [InlineData("{\"form_submit_id\":42}", false)]
    [InlineData("{\"form_submit_id\":43,\"trial_id\":7}", false)]
    [InlineData("{\"form_submit_id\":42,\"trial_id\":8}", false)]
    [InlineData("{\"form_submit_id\":\"42\",\"trial_id\":7}", false)]
    [InlineData("{\"form_submit_id\":42,\"trial_id\":7}", true)]
    [InlineData("{\"trial_id\":7,\"form_submit_id\":42,\"extra\":true}", true)]
    public async Task VerifyMarketingAsync_RequiresMatchingLinkedJsonPayload(string? payload, bool expected)
    {
        await using var connection = await CreateDatabaseAsync();
        await SeedAsync(connection, payload);
        await using var db = Context(connection);

        Assert.Equal(expected, await new MultiTrialNotificationRepository(db).VerifyMarketingAsync(42, 7));
    }

    [Theory]
    [InlineData("update_license", 42, 7, 1)]
    [InlineData("update_marketo", 43, 7, 1)]
    [InlineData("update_marketo", 42, 8, 1)]
    [InlineData("update_marketo", 42, 7, 2)]
    public async Task VerifyMarketingAsync_RejectsWrongTemplateParentOrJsonLink(
        string template, int formId, int trialId, int jsonParentId)
    {
        await using var connection = await CreateDatabaseAsync();
        await SeedAsync(connection, "{\"form_submit_id\":42,\"trial_id\":7}", template, formId, trialId, jsonParentId);
        await using var db = Context(connection);

        Assert.False(await new MultiTrialNotificationRepository(db).VerifyMarketingAsync(42, 7));
    }

    [Fact]
    public async Task VerifyMarketingAsync_RejectsMissingParent()
    {
        await using var connection = await CreateDatabaseAsync();
        await using var db = Context(connection);

        Assert.False(await new MultiTrialNotificationRepository(db).VerifyMarketingAsync(42, 7));
    }

    private static AppDbContext Context(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    private static async Task<SqliteConnection> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            ATTACH DATABASE ':memory:' AS dbo;
            CREATE TABLE dbo.sfdc_template (sfdc_template_id INTEGER PRIMARY KEY, sfdc_template_name TEXT);
            CREATE TABLE dbo.sfdc_opportunity_update
                (sfdc_opportunity_update_id INTEGER PRIMARY KEY, sfdc_template_id INTEGER, form_submit_id INTEGER, trial_id INTEGER);
            CREATE TABLE dbo.sfdc_opportunity_update_json
                (sfdc_opportunity_update_id INTEGER, sfdc_update_json TEXT);
            """;
        await command.ExecuteNonQueryAsync();
        return connection;
    }

    private static async Task SeedAsync(SqliteConnection connection, string? payload,
        string template = "update_marketo", int formId = 42, int trialId = 7, int jsonParentId = 1)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.sfdc_template VALUES (26, @template);
            INSERT INTO dbo.sfdc_opportunity_update VALUES (1, 26, @form, @trial);
            """;
        command.Parameters.AddWithValue("@template", template);
        command.Parameters.AddWithValue("@form", formId);
        command.Parameters.AddWithValue("@trial", trialId);
        await command.ExecuteNonQueryAsync();
        if (payload is not null)
        {
            command.CommandText = "INSERT INTO dbo.sfdc_opportunity_update_json VALUES (@parent, @payload)";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@parent", jsonParentId);
            command.Parameters.AddWithValue("@payload", payload);
            await command.ExecuteNonQueryAsync();
        }
    }
}