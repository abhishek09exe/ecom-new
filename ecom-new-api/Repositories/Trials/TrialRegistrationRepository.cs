using System.Data;
using System.Data.Common;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Repositories.Trials;

/// <summary>
/// Wrapper for dbo.usp_trial_insert_trial_registration — the single database entry point for
/// trial registrations (PHP equivalent: procedures/trial/insert/TrialRegistration.php).
///
/// The procedure is driven almost entirely by @trial_registration_json (parsed with OPENJSON);
/// the scalar parameters are kept for the legacy non-JSON path and are still sent, exactly as
/// the PHP wrapper does.
///
/// A DataReader is used instead of Database.SqlQueryRaw because the procedure returns two
/// different result-set shapes: the success path has no response_code/message columns, while the
/// duplicate (-110) path has no product columns. SqlQueryRaw requires every mapped column to be
/// present in the result set and would throw on one of the two shapes.
/// </summary>
public sealed class TrialRegistrationRepository : ITrialRegistrationRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<TrialRegistrationRepository> _logger;

    public TrialRegistrationRepository(AppDbContext db, ILogger<TrialRegistrationRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TrialRegistrationProcedureResult?> InsertTrialRegistrationAsync(
        MultiTrialSubmissionRequest request,
        string trialRegistrationJson,
        CancellationToken ct = default)
    {
        var connection = _db.Database.GetDbConnection();

        await using var command = connection.CreateCommand();
        command.CommandType = CommandType.StoredProcedure;
        command.CommandText = "dbo.usp_trial_insert_trial_registration";

        AddParameters(command, request, trialRegistrationJson);

        await _db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var reader = await command.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                _logger.LogError(
                    "usp_trial_insert_trial_registration returned no result set for license_category_name={Category}",
                    request.LicenseCategoryName);
                return null;
            }

            return Map(reader);
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private static void AddParameters(DbCommand command, MultiTrialSubmissionRequest r, string trialRegistrationJson)
    {
        void Add(string name, SqlDbType type, int size, object? value)
        {
            var parameter = size > 0
                ? new SqlParameter(name, type, size)
                : new SqlParameter(name, type);
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        Add("@first_name", SqlDbType.NVarChar, 225, r.FirstName);
        Add("@last_name", SqlDbType.NVarChar, 225, r.LastName);
        Add("@company_name", SqlDbType.NVarChar, 255, r.CompanyName);
        Add("@company_type_name", SqlDbType.VarChar, 50, r.CompanyTypeName);
        Add("@phone_number", SqlDbType.VarChar, 64, r.PhoneNumber);
        Add("@customer_email", SqlDbType.VarChar, 100, r.CustomerEmail);
        Add("@address_1", SqlDbType.NVarChar, 255, r.Address1);
        Add("@address_2", SqlDbType.NVarChar, 255, r.Address2);
        Add("@city", SqlDbType.NVarChar, 130, r.City);
        Add("@state", SqlDbType.NVarChar, 2, r.State);
        Add("@postal_code", SqlDbType.NVarChar, 32, r.PostalCode);
        Add("@country", SqlDbType.VarChar, 75, r.Country);
        Add("@language_code", SqlDbType.VarChar, 2, r.LanguageCode);
        Add("@location_code", SqlDbType.VarChar, 3, r.LocationCode);
        Add("@license_category_name", SqlDbType.VarChar, 10, r.LicenseCategoryName);
        Add("@license_seats", SqlDbType.Int, 0, r.LicenseSeats);
        Add("@years", SqlDbType.Float, 0, r.Years);
        Add("@trial_days", SqlDbType.Int, 0, r.TrialDays);
        Add("@license_keycode_type_id", SqlDbType.Int, 0, r.LicenseKeycodeTypeId);
        Add("@partner_key", SqlDbType.VarChar, 36,
            string.IsNullOrWhiteSpace(r.PartnerKey) ? null : r.PartnerKey);
        Add("@account_user_name", SqlDbType.VarChar, 100, r.AccountUserName);
        Add("@partner_account_code", SqlDbType.VarChar, 50, r.PartnerAccountCode);
        Add("@sfdc_lead_id", SqlDbType.VarChar, 18, r.SfdcLeadId);
        Add("@sfdc_opportunity_id", SqlDbType.VarChar, 18, r.SfdcOpportunityId);
        Add("@sfdc_trial_id", SqlDbType.VarChar, 18, r.SfdcTrialId);
        Add("@salesforce_campaign_id", SqlDbType.VarChar, 18, r.SalesforceCampaignId);
        Add("@partner_product_id", SqlDbType.VarChar, 20, r.PartnerProductId);
        Add("@sfdc_distributor_id", SqlDbType.VarChar, 18, r.SfdcDistributorId);
        Add("@sfdc_reseller_id", SqlDbType.VarChar, 18, r.SfdcResellerId);
        Add("@license_distribution_method_code", SqlDbType.Char, 4, r.LicenseDistributionMethodCode);
        Add("@salesforce_license_id", SqlDbType.VarChar, 18, r.SalesforceLicenseId);
        Add("@trial_registration_json", SqlDbType.VarChar, -1, trialRegistrationJson);
        Add("@opt_in", SqlDbType.Int, 0, r.OptIn);
    }

    private static TrialRegistrationProcedureResult Map(DbDataReader reader)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
            columns.Add(reader.GetName(i));

        return new TrialRegistrationProcedureResult
        {
            ResponseCode = GetInt(reader, columns, "response_code"),
            Message = GetString(reader, columns, "message"),
            TrialRegistrationId = GetInt(reader, columns, "trial_registration_id"),
            RequestType = GetString(reader, columns, "request_type"),
            FirstName = GetString(reader, columns, "first_name"),
            LastName = GetString(reader, columns, "last_name"),
            CustomerEmail = GetString(reader, columns, "customer_email"),
            ProductLineId = GetInt(reader, columns, "product_line_id"),
            LicenseCategoryName = GetString(reader, columns, "license_category_name"),
            LicenseCategoryId = GetInt(reader, columns, "license_category_id"),
            LicenseModuleList = GetString(reader, columns, "license_module_list"),
            LicenseType = GetString(reader, columns, "license_type"),
            LicenseTypeId = GetInt(reader, columns, "license_type_id"),
            CapabilityActivationDays = GetInt(reader, columns, "capability_activation_days"),
            LicenseSeats = GetInt(reader, columns, "license_seats"),
            StorageGb = GetInt(reader, columns, "storage_gb"),
            LicenseDistributionMethodCode = GetString(reader, columns, "license_distribution_method_code"),
        };
    }

    private static int? GetInt(DbDataReader reader, HashSet<string> columns, string name)
    {
        var value = GetValue(reader, columns, name);
        return value is null ? null : Convert.ToInt32(value);
    }

    private static string? GetString(DbDataReader reader, HashSet<string> columns, string name)
        => GetValue(reader, columns, name)?.ToString();

    private static object? GetValue(DbDataReader reader, HashSet<string> columns, string name)
    {
        if (!columns.Contains(name)) return null;
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
    }
}
