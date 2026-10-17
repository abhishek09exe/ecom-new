using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.Forms;
using ecom_new_api.Repositories.LicenseOptions;
using ecom_new_api.Repositories.Trials;
using Microsoft.Extensions.Options;

namespace ecom_new_api.Services.MultiTrial;

public sealed class MultiTrialNotificationOptions
{
    public bool EmailEnabled { get; set; }
    public bool MarketingEnabled { get; set; }
}

public interface IMultiTrialNotificationService
{
    Task CompleteAsync(MultiTrialSubmissionRequest request, MultiTrialSubmissionResponse trial,
        string formJson, string? ipAddress, string? formUrl, int? formSubmitId, CancellationToken ct);
}

public sealed class MultiTrialNotificationService(
    IOptions<MultiTrialNotificationOptions> options,
    ILicenseOptionsRepository licenses,
    IFormResponseRepository forms,
    IMultiTrialNotificationRepository notifications,
    ILogger<MultiTrialNotificationService> logger) : IMultiTrialNotificationService
{
    public async Task CompleteAsync(MultiTrialSubmissionRequest request, MultiTrialSubmissionResponse trial,
        string formJson, string? ipAddress, string? formUrl, int? formSubmitId, CancellationToken ct)
    {
        MultiTrialNotificationOptions settings;
        try
        {
            settings = options.Value;
        }
        catch (Exception exception)
        {
            logger.LogError("Multi Trial notification configuration failed for registration {TrialRegistrationId}. ExceptionType={ExceptionType}",
                trial.TrialRegistrationId, exception.GetType().Name);
            return;
        }

        if (settings.EmailEnabled)
        {
            try
            {
                if (!string.Equals(request.LanguageCode, "en", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only confirmed English trial templates are available.");
                var license = await licenses.SelectLicenseOptionsAsync(trial.Keycode!, ct: ct);
                if (license?.LicenseId is not > 0 || !Guid.TryParse(license.LicenseKey, out var licenseKey))
                    throw new InvalidOperationException("Persisted trial license is required for confirmation emails.");
                var categories = license.LicenseProfile?.Values
                    .Select(profile => profile.LicenseCategoryName ?? string.Empty).ToList()
                    ?? throw new InvalidOperationException("Persisted license profile is required for confirmation emails.");
                var bodies = MultiTrialEmailRenderer.Render(request, trial.Keycode!, licenseKey,
                    trial.LicenseSeats ?? request.LicenseSeats, categories);
                await notifications.QueueEmailsAsync(license.LicenseId.Value, request.CustomerEmail!, bodies, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError("Multi Trial confirmation email failed for registration {TrialRegistrationId}. ExceptionType={ExceptionType}",
                    trial.TrialRegistrationId, exception.GetType().Name);
            }
        }

        if (settings.MarketingEnabled)
        {
            try
            {
                formSubmitId ??= await forms.InsertFormSubmitAsync(request.FormName, ipAddress, formUrl, formJson, ct);
                if (formSubmitId is not > 0)
                    throw new InvalidOperationException("A saved form submission is required for marketing.");
                await notifications.QueueMarketingAsync(formSubmitId.Value, trial.TrialId, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError("Multi Trial marketing enqueue failed for registration {TrialRegistrationId}. ExceptionType={ExceptionType}",
                    trial.TrialRegistrationId, exception.GetType().Name);
            }
        }
    }
}