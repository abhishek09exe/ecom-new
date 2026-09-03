using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecom_new_api.Repositories.Pricing;

public sealed class PricingRepository : IPricingRepository
{
    private readonly AppDbContext _ctx;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<PricingRepository> _logger;

    public PricingRepository(AppDbContext ctx, IDbContextFactory<AppDbContext> contextFactory, ILogger<PricingRepository> logger)
        => (_ctx, _contextFactory, _logger) = (ctx, contextFactory, logger);

    /// <inheritdoc/>
    public async Task<List<ConfiguratorPricingResult>> GetItemPricingAsync(
        IReadOnlyList<BundleItemPricingInput> items)
    {
        if (items.Count == 0) return [];

        await using var db = await _contextFactory.CreateDbContextAsync();

        var categoryNames = items.Select(i => i.LicenseCategoryName).Distinct().ToList();
        var yearsValues   = items.Select(i => (double)i.Years).Distinct().ToList();

        // Single batched round trip: all product candidates (across every category name/years
        // combination present in the request) instead of one query chain per item.
        var candidates = await (
            from plc in db.Set<ProductLicenseCategory>()
            join lc in db.Set<LicenseCategory>() on plc.LicenseCategoryId equals lc.LicenseCategoryId
            join p in db.Set<Product>() on plc.ProductId equals p.ProductId
            join pt in db.Set<ProductType>() on p.ProductTypeId equals pt.ProductTypeId
            join pf in db.Set<ProductFamily>() on p.ProductFamilyId equals pf.ProductFamilyId
            join py in db.Set<ProductYears>() on p.ProductId equals py.ProductId
            join ps in db.Set<ProductSeat>() on p.ProductId equals ps.ProductId
            where categoryNames.Contains(lc.LicenseCategoryName)
               && yearsValues.Contains(py.Years)
            select new ProductCandidateRow(
                p.ProductId,
                lc.LicenseCategoryName,
                lc.LicenseCategoryDescription,
                p.LicenseKeycodeTypeId,
                p.ProductDescription,
                pt.ProductTypeDescription,
                pf.ProductFamilyDescription,
                py.Years,
                ps.Seats)
        ).ToListAsync();

        // Resolve the matched product per item purely in-memory against the shared candidate set.
        var matches = new (BundleItemPricingInput Item, ProductCandidateRow? Product)[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var yearsDouble = (double)item.Years;

            var forItem = candidates.Where(c =>
                c.LicenseCategoryName == item.LicenseCategoryName
                && (c.LicenseKeycodeTypeId == item.LicenseKeycodeTypeId || c.LicenseKeycodeTypeId is null)
                && c.Years == yearsDouble);

            // Lowest seat tier that covers the requested seats; fall back to the highest available.
            var matched = forItem.Where(c => c.Seats >= item.LicenseSeats)
                                  .OrderBy(c => c.Seats)
                                  .FirstOrDefault()
                          ?? forItem.OrderByDescending(c => c.Seats).FirstOrDefault();

            matches[i] = (item, matched);
        }

        // Single batched round trip: pricing rows for every matched product id.
        var matchedProductIds = matches.Where(m => m.Product is not null)
                                        .Select(m => m.Product!.ProductId)
                                        .Distinct()
                                        .ToList();

        var pricingRows = matchedProductIds.Count > 0
            ? await db.Set<ProductPricing>()
                .Where(pp => matchedProductIds.Contains(pp.ProductId))
                .ToListAsync()
            : [];

        var results = new List<ConfiguratorPricingResult>();
        foreach (var (item, matchedProduct) in matches)
        {
            if (matchedProduct is null) continue;

            var (langCode, locCode) = ParseLocale(item.Locale);

            // Case-insensitive comparison: SQL Server's default collation matched
            // language/location codes case-insensitively in the old per-item query,
            // so replicate that here now that filtering happens in memory.
            var pricing = pricingRows.FirstOrDefault(pp =>
                pp.ProductId == matchedProduct.ProductId
                && string.Equals(pp.LanguageCode, langCode, StringComparison.OrdinalIgnoreCase)
                && string.Equals(pp.LocationCode, locCode, StringComparison.OrdinalIgnoreCase))
                // Fallback: language match only (some products lack location-specific rows).
                ?? pricingRows.FirstOrDefault(pp =>
                    pp.ProductId == matchedProduct.ProductId
                    && string.Equals(pp.LanguageCode, langCode, StringComparison.OrdinalIgnoreCase));

            if (pricing is null) continue;

            var listPrice   = pricing.RetailPrice;
            var usagePrice  = item.Years > 0 ? Math.Round(listPrice / (decimal)item.Years / 12, 6) : 0m;
            var eqYearPrice = item.Years > 0 ? Math.Round(listPrice / (decimal)item.Years, 6) : listPrice;

            // Resolve start / expiration dates: today → today + years.
            var start      = DateTime.UtcNow.Date;
            var expiration = start.AddYears((int)item.Years);

            results.Add(new ConfiguratorPricingResult
            {
                LineItem                = results.Count + 1,
                Quantity                = item.LicenseSeats,
                ListPrice               = listPrice,
                UnitPrice               = listPrice,
                UsagePrice              = usagePrice,
                EquivalentYearPrice     = eqYearPrice,
                OrderItemOfferAmount    = null,
                ProductDescription      = matchedProduct.ProductDescription ?? string.Empty,
                ProductTypeDescription  = matchedProduct.ProductTypeDescription ?? string.Empty,
                LicenseCategoryName     = item.LicenseCategoryName,
                LicenseCategoryDescription = matchedProduct.LicenseCategoryDescription,
                ProductFamilyDescription = matchedProduct.ProductFamilyDescription,
                StartDate               = start,
                ExpirationDate          = expiration,
                CartItemBundleId        = item.CartItemBundleId,
                ItemHierarchyId         = item.ItemHierarchyId,
                LicenseKeycodeTypeId    = matchedProduct.LicenseKeycodeTypeId ?? item.LicenseKeycodeTypeId,
                DependentCartOrderItemId = null,
                StorageGb               = item.StorageGb,
                UsagePricingModelId     = null,
                RetentionModelId        = item.RetentionModelId,
                RetentionTerm           = null,
                RetentionModelName      = null,
                ActualStorageQuantity   = null,
            });
        }

        return results;
    }

    /// <summary>
    /// Flattened projection of a candidate product joined with its license category, type,
    /// family, years, and seat-tier rows. Used to resolve all items' matches in-memory from a
    /// single batched query instead of one query chain per item.
    /// </summary>
    private sealed record ProductCandidateRow(
        int ProductId,
        string LicenseCategoryName,
        string? LicenseCategoryDescription,
        int? LicenseKeycodeTypeId,
        string? ProductDescription,
        string? ProductTypeDescription,
        string? ProductFamilyDescription,
        double Years,
        int Seats);

    /// <summary>
    /// Splits a locale string such as "en_US" or "en-US" into (languageCode, locationCode).
    /// </summary>
    private static (string lang, string loc) ParseLocale(string locale)
    {
        var parts = locale.Replace('-', '_').Split('_');
        return parts.Length >= 2
            ? (parts[0].ToLowerInvariant(), parts[1].ToUpperInvariant())
            : (parts[0].ToLowerInvariant(), string.Empty);
    }
}

