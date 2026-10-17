using System.Data;
using System.Text.Json;
using ecom_new_api.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ecom_new_api.Repositories.Trials;

public interface IMultiTrialNotificationRepository
{
    Task QueueEmailsAsync(int licenseId, string customerEmail, IReadOnlyList<string> bodies, CancellationToken ct);
    Task QueueMarketingAsync(int formSubmitId, int trialId, CancellationToken ct);
}

public sealed class MultiTrialNotificationRepository(AppDbContext db) : IMultiTrialNotificationRepository
{
    public async Task QueueEmailsAsync(int licenseId, string customerEmail, IReadOnlyList<string> bodies, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        foreach (var body in bodies)
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.usp_insert_email_receipts_dl_instructions";
            command.Parameters.Add(new SqlParameter("@customer_email", SqlDbType.VarChar, 100) { Value = customerEmail });
            command.Parameters.Add(new SqlParameter("@license_id", SqlDbType.Int) { Value = licenseId });
            command.Parameters.Add(new SqlParameter("@order_header_id", SqlDbType.Int) { Value = DBNull.Value });
            command.Parameters.Add(new SqlParameter("@email_message_name", SqlDbType.VarChar, 65)
                { Value = "email_dl_instructions_business_trials" });
            command.Parameters.Add(new SqlParameter("@email_message_body", SqlDbType.NVarChar, -1) { Value = body });
            var messageId = await command.ExecuteScalarAsync(ct);
            if (messageId is null or DBNull || Convert.ToInt32(messageId) <= 0)
                throw new InvalidOperationException("Confirmation email persistence returned no message ID.");
        }
        await transaction.CommitAsync(ct);
    }

    public async Task QueueMarketingAsync(int formSubmitId, int trialId, CancellationToken ct)
    {
        var updateJson = JsonSerializer.Serialize(new { form_submit_id = formSubmitId, trial_id = trialId });
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.usp_sfdc_insert_sfdc_opportunity_update";
            command.Parameters.Add(new SqlParameter("@sfdc_template_name", SqlDbType.VarChar, 50) { Value = "update_marketo" });
            command.Parameters.Add(new SqlParameter("@trial_id", SqlDbType.Int) { Value = trialId });
            command.Parameters.Add(new SqlParameter("@form_submit_id", SqlDbType.Int) { Value = formSubmitId });
            command.Parameters.Add(new SqlParameter("@sfdc_update_json", SqlDbType.NVarChar, -1) { Value = updateJson });
            await command.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        if (!await VerifyMarketingAsync(formSubmitId, trialId, ct))
            throw new InvalidOperationException("Marketing persistence returned no matching queue payload.");
    }

    public async Task<bool> VerifyMarketingAsync(int formSubmitId, int trialId, CancellationToken ct = default)
    {
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var verification = db.Database.GetDbConnection().CreateCommand();
            verification.CommandText = @"
                SELECT j.sfdc_update_json
                FROM dbo.sfdc_opportunity_update u
                INNER JOIN dbo.sfdc_template t ON t.sfdc_template_id = u.sfdc_template_id
                INNER JOIN dbo.sfdc_opportunity_update_json j
                    ON j.sfdc_opportunity_update_id = u.sfdc_opportunity_update_id
                WHERE u.form_submit_id = @form_submit_id AND u.trial_id = @trial_id
                  AND t.sfdc_template_name = 'update_marketo'";
            var formParameter = verification.CreateParameter();
            formParameter.ParameterName = "@form_submit_id";
            formParameter.DbType = DbType.Int32;
            formParameter.Value = formSubmitId;
            verification.Parameters.Add(formParameter);
            var trialParameter = verification.CreateParameter();
            trialParameter.ParameterName = "@trial_id";
            trialParameter.DbType = DbType.Int32;
            trialParameter.Value = trialId;
            verification.Parameters.Add(trialParameter);

            await using var reader = await verification.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (reader.IsDBNull(0))
                    continue;
                try
                {
                    using var document = JsonDocument.Parse(reader.GetString(0));
                    var payload = document.RootElement;
                    if (payload.ValueKind == JsonValueKind.Object &&
                        payload.TryGetProperty("form_submit_id", out var formId) && formId.ValueKind == JsonValueKind.Number &&
                        formId.TryGetInt32(out var savedFormId) && savedFormId == formSubmitId &&
                        payload.TryGetProperty("trial_id", out var trial) && trial.ValueKind == JsonValueKind.Number &&
                        trial.TryGetInt32(out var savedTrialId) && savedTrialId == trialId)
                        return true;
                }
                catch (JsonException)
                {
                }
            }
            return false;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}