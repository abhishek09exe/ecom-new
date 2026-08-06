using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Services.Pricing;

public interface IPricingService
{
    Task<BundlePricingResponse> GetBundlePricingAsync(BundlePricingRequest request);
}
