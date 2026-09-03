using ecom_new_api.Data.Entities;

namespace ecom_new_api.Repositories.Pricing;

public interface IPricingRepository
{
    /// <summary>
    /// Returns pricing rows for the supplied items using pure EF Core queries —
    /// no stored-procedure call required.
    /// </summary>
    Task<List<ConfiguratorPricingResult>> GetItemPricingAsync(
        IReadOnlyList<BundleItemPricingInput> items);
}
