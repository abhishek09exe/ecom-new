using ecom_new_api.Models.Responses;

namespace ecom_new_api.Services.LicenseOptions;

public interface ILicenseOptionsService
{
    Task<ServiceResult<LicenseOptionsResponse>> GetLicenseOptionsByMessageKeyAsync(
        string messageKey,
    string? locale = null,
        CancellationToken ct = default);
}
