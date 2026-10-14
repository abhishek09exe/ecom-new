using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;

namespace ecom_new_api.Repositories.Trials;

public interface ITrialRegistrationRepository
{
    Task<IReadOnlyList<int>> InsertModuleLicenseRecordsAsync(
        int licenseId, LicenseCreationContext creationContext,
        DateTime originalAuditTimestamp, string originalAuditUser, CancellationToken ct = default);

    Task<LicensePrimaryCreationResult> InsertPrimaryLicenseRecordsAsync(
        int licenseId, LicenseCreationContext creationContext,
        DateTime originalAuditTimestamp, string originalAuditUser, CancellationToken ct = default);

    Task<Guid> InsertLicenseKeyAsync(int licenseId, string serviceJson, CancellationToken ct = default);

    Task<int?> InsertLicenseFeatureLicenseAsync(
        int licenseId, LicenseCreationContext creationContext, CancellationToken ct = default);

    Task<LicenseCreationResult> InsertLicenseRecordsInLegacyOrderAsync(
        int licenseId, LicenseCreationContext creationContext, string serviceJson,
        DateTime originalAuditTimestamp, string originalAuditUser, CancellationToken ct = default);

    Task<int> InsertLicenseHeaderAsync(
        LicenseCreationContext creationContext, string preparedKeycode, string serviceJson,
        CancellationToken ct = default);

    Task<LicenseCreationContext> ResolveLicenseCreationContextAsync(
        string serviceJson, CancellationToken ct = default);

    /// <summary>
    /// Executes dbo.usp_trial_insert_trial_registration for a trial submission.
    /// Returns null when the procedure produced no result set (error path).
    /// </summary>
    Task<TrialRegistrationProcedureResult?> InsertTrialRegistrationAsync(
        MultiTrialSubmissionRequest request,
        string trialRegistrationJson,
        CancellationToken ct = default);

    /// <summary>
    /// Executes dbo.usp_trial_insert_trial and returns the created trial identifier plus the
    /// final persisted keycode from trial_registration.
    /// </summary>
    Task<TrialInsertProcedureResult?> InsertTrialAsync(
        int trialRegistrationId,
        string? keycode,
        CancellationToken ct = default);
}
