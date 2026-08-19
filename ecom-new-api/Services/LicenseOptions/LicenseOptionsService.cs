using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.LicenseOptions;

namespace ecom_new_api.Services.LicenseOptions;

public sealed class LicenseOptionsService : ILicenseOptionsService
{
    private readonly ILicenseOptionsRepository _repo;
    private readonly ILogger<LicenseOptionsService> _logger;

    public LicenseOptionsService(ILicenseOptionsRepository repo, ILogger<LicenseOptionsService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<ServiceResult<LicenseOptionsResponse>> GetLicenseOptionsByMessageKeyAsync(
        string messageKey,
        string? locale = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Resolving license options for message_key={MessageKey} locale={Locale}", messageKey, locale);

        var keycode = await _repo.ResolveKeycodeFromMessageKeyAsync(messageKey, ct);
        if (keycode is null)
            return ServiceResult<LicenseOptionsResponse>.NotFound($"No license found for message_key '{messageKey}'");

        var result = await _repo.SelectLicenseOptionsAsync(keycode, locale, ct);
        return result is null
            ? ServiceResult<LicenseOptionsResponse>.NotFound($"No license found for message_key '{messageKey}'")
            : ServiceResult<LicenseOptionsResponse>.Ok(result);
    }
}
