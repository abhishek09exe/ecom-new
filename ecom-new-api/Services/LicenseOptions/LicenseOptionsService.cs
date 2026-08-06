using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.LicenseOptions;

namespace ecom_new_api.Services.LicenseOptions;

public sealed class LicenseOptionsService : ILicenseOptionsService
{
    private readonly ILicenseOptionsRepository _repo;

    public LicenseOptionsService(ILicenseOptionsRepository repo) => _repo = repo;

    public async Task<ServiceResult<LicenseOptionsResponse>> GetLicenseOptionsByMessageKeyAsync(
        string messageKey,
        CancellationToken ct = default)
    {
        var keycode = await _repo.ResolveKeycodeFromMessageKeyAsync(messageKey, ct);
        if (keycode is null)
            return ServiceResult<LicenseOptionsResponse>.NotFound($"No license found for message_key '{messageKey}'");

        var result = await _repo.SelectLicenseOptionsAsync(keycode, ct);
        return result is null
            ? ServiceResult<LicenseOptionsResponse>.NotFound($"No license found for message_key '{messageKey}'")
            : ServiceResult<LicenseOptionsResponse>.Ok(result);
    }
}
