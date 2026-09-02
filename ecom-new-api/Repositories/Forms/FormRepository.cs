using System.Data;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Repositories.Forms;

/// <summary>
/// Persistence for the forms/trial-registration pipeline.
/// Mirrors the legacy procedures:
///   form\insert\FormSubmit          -> usp_form_insert_form_submit
///   form\insert\FormResponse        -> usp_form_insert_form_response
///   trial\insert\TrialRegistration  -> usp_trial_insert_trial_registration
///   skyrise\insert\SkyriseFailure   -> usp_skyrise_insert_license_failure
/// </summary>
public sealed class FormRepository : IFormRepository
{
    private readonly AppDbContext _ctx;
    private readonly ILogger<FormRepository> _logger;

    public FormRepository(AppDbContext ctx, ILogger<FormRepository> logger)
        => (_ctx, _logger) = (ctx, logger);

    public async Task<long> InsertFormSubmitAsync(
        string formName, string? ipAddress, string? formUrl, string formJson, string? insertBy)
    {
        var pFormName = new SqlParameter("@form_name", SqlDbType.VarChar, 100) { Value = formName };
        var pIp = new SqlParameter("@ip_address", SqlDbType.VarChar, 50) { Value = (object?)ipAddress ?? DBNull.Value };
        var pUrl = new SqlParameter("@form_url", SqlDbType.VarChar, 500) { Value = (object?)formUrl ?? DBNull.Value };
        // NVARCHAR(MAX) required; the serialized form payload can exceed 4000 chars.
        var pJson = new SqlParameter("@form_json", SqlDbType.NVarChar, -1) { Value = formJson };
        var pBy = new SqlParameter("@insert_by", SqlDbType.VarChar, 100) { Value = (object?)insertBy ?? DBNull.Value };

        _logger.LogDebug("Executing usp_form_insert_form_submit for {FormName}", formName);

        var rows = await _ctx.Database
            .SqlQueryRaw<FormSubmitInsertResult>(
                "EXEC usp_form_insert_form_submit @form_name, @ip_address, @form_url, @form_json, @insert_by",
                pFormName, pIp, pUrl, pJson, pBy)
            .ToListAsync();

        return rows.FirstOrDefault()?.FormSubmitId ?? 0;
    }

    public async Task<FormResponseInsertResult> InsertFormResponseAsync(long formSubmitId, string formResponse)
    {
        var pId = new SqlParameter("@form_submit_id", SqlDbType.BigInt) { Value = formSubmitId };
        var pResponse = new SqlParameter("@form_response", SqlDbType.NVarChar, -1) { Value = formResponse };

        _logger.LogDebug("Executing usp_form_insert_form_response for submit {FormSubmitId}", formSubmitId);

        var rows = await _ctx.Database
            .SqlQueryRaw<FormResponseInsertResult>(
                "EXEC usp_form_insert_form_response @form_submit_id, @form_response",
                pId, pResponse)
            .ToListAsync();

        return rows.FirstOrDefault() ?? new FormResponseInsertResult();
    }

    public async Task<TrialRegistrationInsertResult?> InsertTrialRegistrationAsync(string trialRegistrationJson)
    {
        var pJson = new SqlParameter("@trial_registration_json", SqlDbType.NVarChar, -1)
        {
            Value = trialRegistrationJson
        };

        _logger.LogDebug("Executing usp_trial_insert_trial_registration");

        var rows = await _ctx.Database
            .SqlQueryRaw<TrialRegistrationInsertResult>(
                "EXEC usp_trial_insert_trial_registration @trial_registration_json",
                pJson)
            .ToListAsync();

        return rows.FirstOrDefault();
    }

    public async Task InsertSkyriseFailureAsync(string skyriseRequest, string skyriseFailureMessage)
    {
        var pRequest = new SqlParameter("@skyrise_request", SqlDbType.NVarChar, -1) { Value = skyriseRequest };
        var pMessage = new SqlParameter("@skyrise_failure_message", SqlDbType.NVarChar, -1)
        {
            Value = skyriseFailureMessage
        };

        _logger.LogDebug("Executing usp_skyrise_insert_license_failure");

        await _ctx.Database.ExecuteSqlRawAsync(
            "EXEC usp_skyrise_insert_license_failure @skyrise_request, @skyrise_failure_message",
            pRequest, pMessage);
    }
}
