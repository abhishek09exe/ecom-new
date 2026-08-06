using ecom_new_api.Data.Entities;

namespace ecom_new_api.Repositories.Pricing;

public interface IPricingRepository
{
    Task<List<ConfiguratorPricingResult>> GetConfiguratorPricingAsync(string itemJson, string bundleJson);
}
