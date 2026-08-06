using ecom_new_api.Models.Responses;

namespace ecom_new_api.Repositories.LicenseOptions;

public interface ILicenseOptionsRepository
{
    Task<string?> ResolveKeycodeFromMessageKeyAsync(string messageKey, CancellationToken ct = default);

    Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(string keycode, CancellationToken ct = default);
}
