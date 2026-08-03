using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Services;

/// <summary>
/// Orchestrates cart order creation and read operations.
/// Sits between the controller (HTTP concerns) and the repository (DB concerns).
/// All business logic — validation, quote-key pivot, vendor order code generation — lives here.
/// </summary>
public interface ICartOrderService
{
    /// <summary>
    /// Creates a new cart order, or updates an existing pending (quote) cart if the
    /// supplied key resolves to an existing one.
    /// Returns the fully hydrated order response (re-read after insert).
    /// </summary>
    Task<ServiceResult<CartOrderResponse>> CreateCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default);

    Task<ServiceResult<LicenseOptionsResponse>> GetLicenseOptionsAsync(
        string keycode, CancellationToken ct = default);

    Task<ServiceResult<LicenseOptionsResponse>> GetLicenseOptionsByMessageKeyAsync(
        string messageKey, CancellationToken ct = default);

    Task<ServiceResult<ConfigureResponse>> GetConfigureAsync(
        string keycode, CancellationToken ct = default);

    Task<ServiceResult<UpgradeResponse>> GetUpgradeAsync(
        string keycode, CancellationToken ct = default);
}
