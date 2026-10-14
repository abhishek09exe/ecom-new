using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ecom_new_api.Data;

namespace ecom_new_api.Repositories.Forms;

public sealed class FormResponseRepository(AppDbContext db) : IFormResponseRepository
{
    public async Task<int?> InsertFormSubmitAsync(
        string formName,
        string? ipAddress,
        string? formUrl,
        string formJson,
        CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.usp_form_insert_form_submit";
            command.Parameters.Add(new SqlParameter("@form_name", SqlDbType.VarChar, 50) { Value = formName });
            command.Parameters.Add(new SqlParameter("@ip_address", SqlDbType.VarChar, 24)
            {
                Value = (object?)ipAddress ?? DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@form_url", SqlDbType.VarChar, 500)
            {
                Value = (object?)formUrl ?? DBNull.Value
            });
            command.Parameters.Add(new SqlParameter("@form_json", SqlDbType.NVarChar, -1) { Value = formJson });

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            var idOrdinal = reader.GetOrdinal("form_submit_id");
            return reader.IsDBNull(idOrdinal) ? null : reader.GetInt32(idOrdinal);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public async Task<string?> InsertFormResponseAsync(
        int formSubmitId,
        string responseJson,
        CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.usp_form_insert_form_response";
            command.Parameters.Add(new SqlParameter("@form_submit_id", SqlDbType.Int) { Value = formSubmitId });
            command.Parameters.Add(new SqlParameter("@form_response", SqlDbType.NVarChar, -1) { Value = responseJson });

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            var keyOrdinal = reader.GetOrdinal("form_response_key");
            if (reader.IsDBNull(keyOrdinal))
                return null;

            var responseKey = reader.GetString(keyOrdinal);
            return Guid.TryParseExact(responseKey, "D", out _) ? responseKey : null;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public async Task<FormResponseSnapshot?> GetFormResponseAsync(
        string responseKey,
        CancellationToken ct = default)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "dbo.usp_form_select_form_response";
            command.Parameters.Add(new SqlParameter("@form_response_key", SqlDbType.VarChar, 36)
            {
                Value = responseKey
            });

            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
                return null;

            return new FormResponseSnapshot(
                reader.GetInt32(reader.GetOrdinal("form_response_id")),
                reader.GetInt32(reader.GetOrdinal("form_submit_id")),
                reader.GetString(reader.GetOrdinal("form_response_key")),
                reader.GetString(reader.GetOrdinal("form_response")),
                reader.GetDateTime(reader.GetOrdinal("insert_date")));
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }
}