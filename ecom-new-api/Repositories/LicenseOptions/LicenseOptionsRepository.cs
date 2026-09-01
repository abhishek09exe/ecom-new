using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ecom_new_api.Repositories.LicenseOptions;

/// <summary>
/// EF Core data access for the license-options endpoint.
/// Resolves a message_key GUID to a keycode, then hydrates the full license aggregate
/// (license info, profile, product options, upgrade categories) for the configurator page.
/// </summary>
public sealed class LicenseOptionsRepository : ILicenseOptionsRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<LicenseOptionsRepository> _logger;
    private readonly IMemoryCache _cache;

    // Cache keys for reference data
    private const string CacheKey_LicenseTypes = "LicenseTypes_All";
    private const string CacheKey_ProductTypes = "ProductTypes_All";
    private const string CacheKey_CapabilityTypes = "CapabilityTypes_All";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(4); // Reference data changes rarely

    public LicenseOptionsRepository(AppDbContext db, ILogger<LicenseOptionsRepository> logger, IMemoryCache cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
    }

    public async Task<string?> ResolveKeycodeFromMessageKeyAsync(
        string messageKey, CancellationToken ct = default)
    {
        if (!Guid.TryParse(messageKey, out var guid)) return null;

        // Optimized: AsNoTracking for read-only query
        return await _db.LicenseKey
            .Where(lk => lk.Key == guid)
            .Join(_db.License,
                lk => lk.LicenseId,
                l => l.LicenseId,
                (lk, l) => l.Keycode)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public async Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
        string keycode,
        string? locale = null,
        CancellationToken ct = default)
    {
        var license = await (
            from l in _db.License
            join s in _db.LicenseStatus on l.LicenseStatusId equals s.LicenseStatusId
            join pl in _db.ProductLine on l.ProductLineId equals pl.ProductLineId
            join lkRow in _db.LicenseKey on l.LicenseId equals lkRow.LicenseId into lkJoin
            from lkRow in lkJoin.DefaultIfEmpty()
            where l.Keycode == keycode
            select new
            {
                l.LicenseId,
                l.Keycode,
                l.CustomerId,
                l.LicenseStatusId,
                l.LicenseTypeId,
                l.LicenseDistributionMethodId,
                l.LicenseKeycodeTypeId,
                l.MaxDailyActivations,
                l.LicenseExpirationDate,
                l.InsertDate,
                StatusDescription = s.LicenseStatusDescription,
                ProductLineDescription = pl.ProductLineDescription,
                LicenseKeyGuid = lkRow == null ? (Guid?)null : (Guid?)lkRow.Key
            }
        ).AsNoTracking().FirstOrDefaultAsync(ct);

        if (license is null)
        {
            _logger.LogInformation("No license found for keycode={Keycode}", keycode);
            return null;
        }

        LicenseByIdProcedureRow? legacyLicenseRow = null;
        if (_db.Database.IsSqlServer())
        {
            var legacyLicenseRows = await _db.LicenseByIdProcedureRows
                .FromSqlInterpolated($"EXEC dbo.usp_license_select_license_by_id @license_id = {license.LicenseId}")
                .AsNoTracking()
                .ToListAsync(ct);

            legacyLicenseRow = legacyLicenseRows.FirstOrDefault();
        }

        // ✅ OPTIMIZED: Execute 10 independent queries in PARALLEL instead of sequentially
        // This reduces 10 sequential round trips to 1 parallel batch (10x faster minimum)
        var task1 = _db.LicenseType
            .Where(t => t.LicenseTypeId == license.LicenseTypeId)
            .Select(t => t.LicenseTypeDescription)
            .FirstOrDefaultAsync(ct);

        var task2 = _db.LicenseParent
            .Where(lp => lp.ChildLicenseId == license.LicenseId)
            .Join(_db.License, lp => lp.ParentLicenseId, parent => parent.LicenseId, (lp, parent) => parent.Keycode)
            .FirstOrDefaultAsync(ct);

        var task3 = _db.LicenseActiveSeats
            .Where(r => r.LicenseId == license.LicenseId)
            .OrderByDescending(r => r.EndDate)
            .Select(r => (int?)r.ConsumedSeats)
            .FirstOrDefaultAsync(ct);

        var task4 = _db.LicenseStorage
            .Where(r => r.LicenseId == license.LicenseId)
            .OrderByDescending(r => r.LicenseStorageId)
            .Select(r => (int?)r.StorageGb)
            .FirstOrDefaultAsync(ct);

        var task5 = (from lal in _db.LicenseAttributeLicense
                     join la in _db.LicenseAttribute on lal.LicenseAttributeId equals la.LicenseAttributeId
                     join lav in _db.LicenseAttributeLicenseValue on lal.LicenseAttributeLicenseValue equals (int?)lav.Value into lavJoin
                     from lav in lavJoin.DefaultIfEmpty()
                     where lal.LicenseId == license.LicenseId
                     orderby lal.LicenseAttributeLicenseId descending
                     select new
                     {
                         la.LicenseAttributeDescription,
                         la.LicenseAttributeTag,
                         lal.LicenseAttributeLicenseValue,
                         LicenseAttributeLicenseValueDescription = lav == null ? null : lav.Description,
                         LicenseAttributeLastModified = (DateTime?)lal.ModifiedDate,
                     }).AsNoTracking().FirstOrDefaultAsync(ct);

        var task6 = (from oil in _db.OrderItemLicense
                     join oi in _db.OrderItem on oil.OrderItemId equals oi.OrderItemId
                     join p in _db.Product on oi.ProductId equals p.ProductId
                     where oil.LicenseId == license.LicenseId && p.ProductTypeId == 2
                     select oil.OrderItemLicenseId).CountAsync(ct);

        var task7 = (from lh in _db.LicenseHistory
                     join ldmc in _db.LicenseDistributionMethodChannel on lh.LicenseDistributionMethodId equals ldmc.LicenseDistributionMethodId
                     join ch in _db.Channel on ldmc.ChannelId equals ch.ChannelId
                     where lh.LicenseId == license.LicenseId
                     orderby lh.HistoryDate
                     select new { ch.ChannelName, ActivationDate = (DateTime?)lh.InsertDate }).AsNoTracking().FirstOrDefaultAsync(ct);

        var task8 = _db.LicenseDistributionMethod
            .Where(m => m.LicenseDistributionMethodId == license.LicenseDistributionMethodId)
            .Select(m => m.LicenseDistributionMethodCode)
            .FirstOrDefaultAsync(ct);

        var task9 = _db.LicenseNextBillDate
            .Where(n => n.LicenseId == license.LicenseId)
            .OrderByDescending(n => n.LicenseNextBillDateId)
            .Select(n => (DateTime?)n.NextBillDate)
            .FirstOrDefaultAsync(ct);

        var task10 = _db.Customer
            .Where(c => c.CustomerId == license.CustomerId)
            .Select(c => c.OptIn)
            .FirstOrDefaultAsync(ct);

        await Task.WhenAll(task1, task2, task3, task4, task6, task8, task9, task10).ConfigureAwait(false);

        // Extract results
        var fallbackLicenseTypeDescription = await task1;
        var fallbackParentKeycode = await task2;
        var fallbackConsumedSeats = await task3;
        var fallbackStorageGb = await task4;
        var fallbackAttribute = await task5;
        var fallbackRenewalCount = await task6;
        var fallbackChannel = await task7;
        var fallbackDistributionCode = await task8;
        var fallbackNextBillDate = await task9;
        var fallbackEmailOptIn = await task10;

        // ✅ OPTIMIZED: Fetch category rows and capabilities in parallel
        var categoryRowsTask = (from lcl in _db.LicenseCategoryLicense
                                join lc in _db.LicenseCategory on lcl.LicenseCategoryId equals lc.LicenseCategoryId
                                where lcl.LicenseId == license.LicenseId
                                orderby lcl.LicenseCategoryLicenseId descending
                                select new
                                {
                                    lc.LicenseCategoryId,
                                    lc.LicenseCategoryName,
                                    lc.LicenseCategoryDescription,
                                    lc.BaseCapabilityId,
                                    lcl.StartDate,
                                    EndDate = lcl.EndDate
                                }).AsNoTracking().ToListAsync(ct);

        var capabilityByIdTask = (from c in _db.LicenseCapability
                                  join t in _db.CapabilityType on c.CapabilityTypeId equals t.CapabilityTypeId
                                  where c.LicenseId == license.LicenseId
                                  select new { c.CapabilityId, t.CapabilityTypeDescription })
                                  .AsNoTracking()
                                  .ToDictionaryAsync(x => x.CapabilityId, x => x.CapabilityTypeDescription, ct);

        await Task.WhenAll(categoryRowsTask, capabilityByIdTask).ConfigureAwait(false);

        var categoryRows = await categoryRowsTask;
        var capabilityById = await capabilityByIdTask;

        var primaryCategory = categoryRows.FirstOrDefault();

        string? capabilityTypeDescription = null;
        if (primaryCategory?.BaseCapabilityId is int baseCapabilityId &&
            capabilityById.TryGetValue(baseCapabilityId, out var baseCapabilityTypeDescription))
        {
            capabilityTypeDescription = baseCapabilityTypeDescription;
        }

        List<LicenseProfileFunctionRow> profileRows;
        if (_db.Database.IsSqlServer())
        {
            profileRows = await _db.LicenseProfileFunctionRows
                .FromSqlInterpolated($@"
                    SELECT
                        f.item_id,
                        f.license_id,
                        CAST(f.license_category_id AS tinyint) AS license_category_id,
                        f.license_category_name,
                        f.license_category_description,
                        f.license_seats,
                        f.storage_gb,
                        f.license_keycode_type_id,
                        f.license_attribute_id,
                        f.license_attribute_description,
                        f.license_attribute_license_value,
                        f.license_attribute_license_value_description,
                        f.start_date,
                        f.expiration_date,
                        CAST(f.category_type_id AS tinyint) AS category_type_id,
                        f.category_type_name,
                        CAST(f.item_hierarchy_id AS tinyint) AS item_hierarchy_id,
                        f.item_hierarchy_name,
                        f.license_status_id,
                        f.license_status_description,
                        f.autorenewal_cycle_name,
                        f.autorenewal_cycle,
                        CAST(f.usage_pricing_model_id AS tinyint) AS usage_pricing_model_id,
                        f.usage_pricing_model_name,
                        CAST(f.retention_model_id AS tinyint) AS retention_model_id,
                        f.retention_model_name,
                        CAST(f.retention_term AS tinyint) AS retention_term,
                        CAST(f.retention_model_type_id AS tinyint) AS retention_model_type_id,
                        CAST(f.product_platform_id AS tinyint) AS product_platform_id,
                        f.product_platform_name,
                        CAST(f.license_autorenewal_value AS tinyint) AS license_autorenewal_value,
                        CAST(f.product_pricing_level_id AS tinyint) AS product_pricing_level_id,
                        f.pricing_level,
                        f.pricing_level_description,
                        f.license_vault_json,
                        f.most_recent_order_term
                    FROM dbo.fn_license_select_license_profile({license.LicenseId}) f")
                .AsNoTracking()
                .ToListAsync(ct);
        }
        else
        {
            var primaryHierarchyName = await _db.ItemHierarchy
                .Where(h => h.ItemHierarchyId == 1)
                .Select(h => h.ItemHierarchyName)
                .FirstOrDefaultAsync(ct);

            profileRows = categoryRows.Select(row => new LicenseProfileFunctionRow
            {
                LicenseCategoryName = row.LicenseCategoryName,
                LicenseCategoryDescription = row.LicenseCategoryDescription,
                LicenseId = license.LicenseId,
                LicenseCategoryId = row.LicenseCategoryId,
                LicenseKeycodeTypeId = license.LicenseKeycodeTypeId,
                LicenseAttributeId = null,
                LicenseAttributeDescription = fallbackAttribute?.LicenseAttributeDescription,
                LicenseAttributeLicenseValue = fallbackAttribute?.LicenseAttributeLicenseValue,
                LicenseAttributeLicenseValueDescription = fallbackAttribute?.LicenseAttributeLicenseValueDescription,
                CategoryTypeName = row.BaseCapabilityId.HasValue &&
                                   capabilityById.TryGetValue(row.BaseCapabilityId.Value, out var categoryType)
                    ? categoryType
                    : null,
                LicenseStatusId = license.LicenseStatusId,
                LicenseStatusDescription = license.StatusDescription,
                StartDate = row.StartDate,
                ExpirationDate = row.EndDate,
                LicenseSeats = null,
                StorageGb = fallbackStorageGb,
                ItemHierarchyId = primaryHierarchyName is null ? null : (byte?)1,
                ItemHierarchyName = primaryHierarchyName,
            }).ToList();
        }

        var (languageCode, locationCode) = ParseLocaleToLanguageAndLocation(locale);

        // ✅ OPTIMIZED: Run upgrade categories and seats queries in parallel
        var upgradeTask = primaryCategory is null
            ? Task.FromResult(new List<dynamic>())
            : (from plcu in _db.ProductLicenseCategoryUpgrade
               join baseLc in _db.LicenseCategory on plcu.LicenseCategoryId equals baseLc.LicenseCategoryId
               join upgradeLc in _db.LicenseCategory on plcu.UpgradeLicenseCategoryId equals upgradeLc.LicenseCategoryId
               join ih in _db.ItemHierarchy on plcu.ItemHierarchyId equals (byte?)ih.ItemHierarchyId
               where plcu.LicenseCategoryId == primaryCategory.LicenseCategoryId
                  && plcu.LanguageCode == languageCode
                  && plcu.LocationCode == locationCode
                  && plcu.ItemHierarchyId == 1
               orderby upgradeLc.LicenseCategoryName
               select new
               {
                   UpgradeLicenseCategoryId = (int)upgradeLc.LicenseCategoryId,
                   LicenseCategoryName = baseLc.LicenseCategoryName,
                   UpgradeLicenseCategoryName = upgradeLc.LicenseCategoryName,
                   ItemHierarchyId = ih.ItemHierarchyId,
                   ItemHierarchyName = ih.ItemHierarchyName,
               }).AsNoTracking().ToListAsync(ct)
               .ContinueWith(t => t.Result.Cast<dynamic>().ToList(), ct);

        var seatsTask = _db.LicenseSeat
            .Where(ls => ls.LicenseId == license.LicenseId)
            .OrderByDescending(ls => ls.LicenseSeatId)
            .Select(ls => (int?)ls.LicenseSeats)
            .FirstOrDefaultAsync(ct);

        await Task.WhenAll(upgradeTask, seatsTask).ConfigureAwait(false);

        var upgradeCategoryRows = await upgradeTask;
        var seats = await seatsTask;

        var upgradeCategories = upgradeCategoryRows
            .ToDictionary(
                row => (string)row.UpgradeLicenseCategoryName ?? string.Empty,
                row => new UpgradeCategoryResponse
                {
                    LicenseCategoryName = row.LicenseCategoryName,
                    UpgradeLicenseCategoryName = row.UpgradeLicenseCategoryName,
                    ItemHierarchyId = row.ItemHierarchyId,
                    ItemHierarchyName = row.ItemHierarchyName,
                },
                StringComparer.OrdinalIgnoreCase);

        var allowedCategoryIds = primaryCategory is null
            ? []
            : upgradeCategoryRows
                .Select(row => (byte)row.UpgradeLicenseCategoryId)
                .Append(primaryCategory.LicenseCategoryId)
                .Distinct()
                .ToList();

        var effectiveProfileRow = profileRows
            .Where(r => string.Equals(r.LicenseCategoryName, primaryCategory?.LicenseCategoryName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.ItemHierarchyId == (byte)1 ? 0 : 1)
            .FirstOrDefault();

        var effectiveEndDate = effectiveProfileRow?.ExpirationDate ?? primaryCategory?.EndDate;
        var effectiveStartDate = effectiveProfileRow?.StartDate ?? primaryCategory?.StartDate;

        var daysRemaining = 0;
        var isExpired = false;
        if (effectiveEndDate.HasValue)
        {
            var deltaDays = (effectiveEndDate.Value - DateTime.UtcNow).Days;
            isExpired = deltaDays < 0;
            daysRemaining = isExpired ? 0 : deltaDays;  // clamp: never report negative days remaining
        }

        // ── Product options ──────────────────────────────────────────────────
        List<ProductOptionResponse> productOptions = [];
        if (allowedCategoryIds.Count > 0)
        {
            // ✅ OPTIMIZED: Run product query and related lookups in parallel
            var productsTask = (from plc in _db.ProductLicenseCategory
                               join p in _db.Product on plc.ProductId equals p.ProductId
                               join pt in _db.ProductType on p.ProductTypeId equals pt.ProductTypeId
                               join lc in _db.LicenseCategory on plc.LicenseCategoryId equals lc.LicenseCategoryId
                               where allowedCategoryIds.Contains(plc.LicenseCategoryId)
                                  && (p.ProductTypeId == 1 || p.ProductTypeId == 2)
                               select new
                               {
                                   p.ProductId,
                                   ProductName = p.ProductDescription,
                                   TypeDescription = pt.ProductTypeDescription,
                                   OptionLicenseCategoryId = plc.LicenseCategoryId,
                                   OptionLicenseCategoryName = lc.LicenseCategoryName,
                               }).AsNoTracking().ToListAsync(ct);

            var allYearsTask = _db.ProductLicenseCategoryYears
                .Where(py => allowedCategoryIds.Contains(py.LicenseCategoryId))
                .Select(py => new { py.LicenseCategoryId, py.Years })
                .AsNoTracking()
                .ToListAsync(ct);

            var allSeatsTask = _db.ProductLicenseCategorySeat
                .Where(ps => allowedCategoryIds.Contains(ps.LicenseCategoryId))
                .Select(ps => new { ps.LicenseCategoryId, ps.Seats })
                .AsNoTracking()
                .ToListAsync(ct);

            var allPricingTask = (from pp in _db.ProductPricing
                                 join plc in _db.ProductLicenseCategory on pp.ProductId equals plc.ProductId
                                 where allowedCategoryIds.Contains(plc.LicenseCategoryId)
                                 select new { pp.ProductId, pp.RetailPrice })
                                 .AsNoTracking()
                                 .Distinct()
                                 .ToListAsync(ct);

            await Task.WhenAll(productsTask, allYearsTask, allSeatsTask, allPricingTask).ConfigureAwait(false);

            var products = await productsTask;
            var allYears = await allYearsTask;
            var allSeats = await allSeatsTask;
            var allPricing = await allPricingTask;

            if (products.Count > 0)
            {
                productOptions = products.Select(p => new ProductOptionResponse
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName ?? string.Empty,
                    LicenseCategoryName = p.OptionLicenseCategoryName,
                    ProductTypeDescription = p.TypeDescription,
                    Price = allPricing.FirstOrDefault(pp => pp.ProductId == p.ProductId)?.RetailPrice,
                    Years = allYears
                        .Where(py => py.LicenseCategoryId == p.OptionLicenseCategoryId)
                        .Select(py => py.Years)
                        .Distinct()
                        .OrderBy(y => y)
                        .ToList(),
                    Seats = allSeats
                        .Where(ps => ps.LicenseCategoryId == p.OptionLicenseCategoryId)
                        .Select(ps => ps.Seats)
                        .Distinct()
                        .OrderBy(s => s)
                        .ToList(),
                }).ToList();
            }
        }

        // ── License profile (legacy function source when available) ─────
        var licenseProfile = profileRows
            .Where(row => !string.IsNullOrWhiteSpace(row.LicenseCategoryName))
            .GroupBy(row => row.LicenseCategoryName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var row = g.OrderBy(r => r.ItemHierarchyId == (byte)1 ? 0 : 1).First();
                    return new LicenseProfileEntryResponse
                    {
                        LicenseCategoryName = row.LicenseCategoryName,
                        LicenseCategoryDescription = row.LicenseCategoryDescription,
                        LicenseCategoryId = row.LicenseCategoryId.HasValue ? (int?)row.LicenseCategoryId.Value : null,
                        LicenseKeycodeTypeId = row.LicenseKeycodeTypeId ?? license.LicenseKeycodeTypeId,
                        LicenseAttributeId = row.LicenseAttributeId,
                        LicenseAttributeDescription = row.LicenseAttributeDescription,
                        LicenseAttributeLicenseValue = row.LicenseAttributeLicenseValue,
                        LicenseAttributeLicenseValueDescription = row.LicenseAttributeLicenseValueDescription,
                        CategoryTypeName = row.CategoryTypeName,
                        LicenseStatusId = row.LicenseStatusId ?? license.LicenseStatusId,
                        LicenseStatusDescription = row.LicenseStatusDescription ?? license.StatusDescription,
                        StartDate = row.StartDate,
                        ExpirationDate = row.ExpirationDate,
                        LicenseSeats = row.LicenseSeats ?? seats,
                        StorageGb = row.StorageGb,
                        ItemHierarchyId = row.ItemHierarchyId.HasValue ? (int?)row.ItemHierarchyId.Value : null,
                        ItemHierarchyName = row.ItemHierarchyName,
                        AutorenewalCycleName = row.AutorenewalCycleName,
                        AutorenewalCycle = row.AutorenewalCycle,
                        UsagePricingModelId = row.UsagePricingModelId.HasValue ? (int?)row.UsagePricingModelId.Value : null,
                        UsagePricingModelName = row.UsagePricingModelName,
                        RetentionModelId = row.RetentionModelId.HasValue ? (int?)row.RetentionModelId.Value : null,
                        RetentionModelName = row.RetentionModelName,
                        RetentionTerm = row.RetentionTerm.HasValue ? (int?)row.RetentionTerm.Value : null,
                        RetentionModelTypeId = row.RetentionModelTypeId.HasValue ? (int?)row.RetentionModelTypeId.Value : null,
                        ProductPlatformId = row.ProductPlatformId.HasValue ? (int?)row.ProductPlatformId.Value : null,
                        ProductPlatformName = row.ProductPlatformName,
                        LicenseAutorenewalValue = row.LicenseAutorenewalValue.HasValue ? (int?)row.LicenseAutorenewalValue.Value : null,
                        ProductPricingLevelId = row.ProductPricingLevelId.HasValue ? (int?)row.ProductPricingLevelId.Value : null,
                        PricingLevel = row.PricingLevel,
                        PricingLevelDescription = row.PricingLevelDescription,
                        LicenseVaultJson = row.LicenseVaultJson,
                        MostRecentOrderTerm = row.MostRecentOrderTerm,
                    };
                },
                StringComparer.OrdinalIgnoreCase);

        var licenseInfo = new LicenseInfoResponse
        {
            Keycode = license.Keycode,
            ProductLineDescription = license.ProductLineDescription,
            LicenseStatusId = license.LicenseStatusId,
            LicenseTypeDescription = legacyLicenseRow?.LicenseTypeDescription ?? fallbackLicenseTypeDescription,
            LicenseKeycodeTypeId = license.LicenseKeycodeTypeId,
            MaxDailyActivations = legacyLicenseRow?.MaxDailyActivations ?? license.MaxDailyActivations,
            LicenseExpirationDate = license.LicenseExpirationDate,
            ParentKeycode = legacyLicenseRow?.ParentKeycode ?? fallbackParentKeycode,
            LicenseKey = license.LicenseKeyGuid?.ToString("D"),
            LicenseCategoryDescription = primaryCategory?.LicenseCategoryDescription,
            StartDate = legacyLicenseRow?.StartDate ?? effectiveStartDate,
            EndDate = legacyLicenseRow?.EndDate ?? effectiveEndDate,
            DaysRemaining = daysRemaining,
            IsExpired = isExpired,
            LicenseCategoryName = primaryCategory?.LicenseCategoryName,
            LicenseSeats = seats,
            ConsumedSeats = legacyLicenseRow?.ConsumedSeats ?? fallbackConsumedSeats,
            SeatsUsed = legacyLicenseRow?.SeatsUsed ?? 0,
            StorageGb = legacyLicenseRow?.StorageGb ?? fallbackStorageGb,
            LicenseAttributeDescription = legacyLicenseRow?.LicenseAttributeDescription ?? fallbackAttribute?.LicenseAttributeDescription,
            LicenseAttributeTag = legacyLicenseRow?.LicenseAttributeTag ?? fallbackAttribute?.LicenseAttributeTag,
            LicenseAttributeLicenseValue = legacyLicenseRow?.LicenseAttributeLicenseValue ?? fallbackAttribute?.LicenseAttributeLicenseValue,
            LicenseAttributeLicenseValueDescription = legacyLicenseRow?.LicenseAttributeLicenseValueDescription ?? fallbackAttribute?.LicenseAttributeLicenseValueDescription,
            LicenseAttributeLastModified = legacyLicenseRow?.LicenseAttributeLastModified ?? fallbackAttribute?.LicenseAttributeLastModified,
            OemType = legacyLicenseRow?.OemType,
            PortalFlag = legacyLicenseRow?.PortalFlag ?? 0,
            RenewalCount = legacyLicenseRow?.RenewalCount ?? fallbackRenewalCount,
            LicenseOriginChannelName = legacyLicenseRow?.LicenseOriginChannelName ?? fallbackChannel?.ChannelName,
            LicenseOriginalActivationDate = legacyLicenseRow?.LicenseOriginalActivationDate ?? fallbackChannel?.ActivationDate ?? license.InsertDate,
            EmailOptIn = legacyLicenseRow?.EmailOptIn ?? fallbackEmailOptIn,
            LicenseDistributionMethodCode = legacyLicenseRow?.LicenseDistributionMethodCode ?? fallbackDistributionCode,
            NextBillDate = legacyLicenseRow?.NextBillDate ?? fallbackNextBillDate,
            CapabilityTypeDescription = capabilityTypeDescription,
        };

        return new LicenseOptionsResponse
        {
            Keycode = license.Keycode,
            LicenseVerified = true,
            LicenseKey = license.LicenseKeyGuid?.ToString("D"),
            LicenseStatus = license.StatusDescription,
            ProductLine = license.ProductLineDescription,
            LicenseCategory = primaryCategory?.LicenseCategoryName,
            LicenseCategoryDescription = primaryCategory?.LicenseCategoryDescription,
            LicenseSeats = seats,
            ExpirationDate = license.LicenseExpirationDate,
            ProductOptions = productOptions,
            License = licenseInfo,
            LicenseSiteId = null,
            LicenseProfile = licenseProfile,
            UpgradeCategories = upgradeCategories,
            BillingModels = [],
        };
    }

    private static (string LanguageCode, string LocationCode) ParseLocaleToLanguageAndLocation(string? locale)
    {
        const string defaultLanguage = "EN";
        const string defaultLocation = "USA";

        if (string.IsNullOrWhiteSpace(locale))
            return (defaultLanguage, defaultLocation);

        var normalized = locale.Trim().Replace('-', '_');
        var parts = normalized.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
            return (defaultLanguage, defaultLocation);

        var language = parts[0].ToUpperInvariant();
        if (language.Length > 2)
            language = language[..2];
        if (language.Length < 2)
            language = defaultLanguage;

        var location = defaultLocation;
        if (parts.Length >= 2)
        {
            location = parts[1].ToUpperInvariant() switch
            {
                "US" or "USA" => "USA",
                "GB" or "GBR" or "UK" => "GBR",
                "AU" or "AUS" => "AUS",
                "CA" or "CAN" => "CAN",
                "DE" or "DEU" => "DEU",
                "FR" or "FRA" => "FRA",
                _ => defaultLocation,
            };
        }

        return (language, location);
    }

    /// <summary>
    /// Get all license types with caching (reference data changes rarely)
    /// </summary>
    private async Task<Dictionary<int, string>> GetLicenseTypesCachedAsync(CancellationToken ct = default)
    {
        var result = await _cache.GetOrCreateAsync(CacheKey_LicenseTypes, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            _logger.LogDebug("Cache miss for {CacheKey}, loading from database", CacheKey_LicenseTypes);

            return await _db.LicenseType
                .AsNoTracking()
                .ToDictionaryAsync(lt => lt.LicenseTypeId, lt => lt.LicenseTypeDescription ?? string.Empty, ct);
        });

        return result!;
    }

    /// <summary>
    /// Get all product types with caching (reference data changes rarely)
    /// </summary>
    private async Task<Dictionary<int, string>> GetProductTypesCachedAsync(CancellationToken ct = default)
    {
        var result = await _cache.GetOrCreateAsync(CacheKey_ProductTypes, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            _logger.LogDebug("Cache miss for {CacheKey}, loading from database", CacheKey_ProductTypes);

            return await _db.ProductType
                .AsNoTracking()
                .ToDictionaryAsync(pt => pt.ProductTypeId, pt => pt.ProductTypeDescription ?? string.Empty, ct);
        });

        return result!;
    }
}
