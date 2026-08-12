using ecom_new_api.Models.Responses;
using ecom_new_api.Services.CartOrders;

namespace ecom_new_api.Services.LicenseOptions;

public sealed class LicenseOptionsService : ILicenseOptionsService
{
    private readonly ICartOrderService _cartOrderService;

    public LicenseOptionsService(ICartOrderService cartOrderService) => _cartOrderService = cartOrderService;

    public async Task<ServiceResult<LicenseOptionsResponse>> GetLicenseOptionsByMessageKeyAsync(
        string messageKey,
        string? locale = null,
        CancellationToken ct = default)
    {
        return await _cartOrderService.GetLicenseOptionsByMessageKeyAsync(messageKey, locale, ct);
    }
}
