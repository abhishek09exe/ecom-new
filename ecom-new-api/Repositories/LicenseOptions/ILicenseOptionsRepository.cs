using ecom_new_api.Models.Responses;

namespace ecom_new_api.Repositories.LicenseOptions;

/// <summary>
/// Data-access contract for the license-options endpoint.
/// Resolves a message_key GUID to a keycode and hydrates the full license aggregate.
/// </summary>
public interface ILicenseOptionsRepository
{
    Task<string?> ResolveKeycodeFromMessageKeyAsync(string messageKey, CancellationToken ct = default);

    Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
        string keycode, string? locale = null, CancellationToken ct = default);
}
