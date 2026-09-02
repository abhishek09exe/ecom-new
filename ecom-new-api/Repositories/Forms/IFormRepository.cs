using ecom_new_api.Data.Entities;

namespace ecom_new_api.Repositories.Forms;

public interface IFormRepository
{
    /// <summary>Executes usp_form_insert_form_submit and returns the new form_submit_id.</summary>
    Task<long> InsertFormSubmitAsync(string formName, string? ipAddress, string? formUrl, string formJson, string? insertBy);

    /// <summary>Executes usp_form_insert_form_response and returns the generated response key.</summary>
    Task<FormResponseInsertResult> InsertFormResponseAsync(long formSubmitId, string formResponse);

    /// <summary>Executes usp_trial_insert_trial_registration with the built trial registration JSON.</summary>
    Task<TrialRegistrationInsertResult?> InsertTrialRegistrationAsync(string trialRegistrationJson);

    /// <summary>Executes usp_skyrise_insert_license_failure when Skyrise keycode generation fails.</summary>
    Task InsertSkyriseFailureAsync(string skyriseRequest, string skyriseFailureMessage);
}
