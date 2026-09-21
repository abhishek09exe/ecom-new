using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;

namespace ecom_new_api.Repositories.Trials;

public interface ITrialRegistrationRepository
{
    /// <summary>
    /// Executes dbo.usp_trial_insert_trial_registration for a trial submission.
    /// Returns null when the procedure produced no result set (error path).
    /// </summary>
    Task<TrialRegistrationProcedureResult?> InsertTrialRegistrationAsync(
        MultiTrialSubmissionRequest request,
        string trialRegistrationJson,
        CancellationToken ct = default);
}
