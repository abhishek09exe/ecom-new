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
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<LicenseOptionsRepository> _logger;
    private readonly IMemoryCache _cache;

    // Cache keys for reference data
    private const string CacheKey_LicenseTypes = "LicenseTypes_All";
    private const string CacheKey_ProductTypes = "ProductTypes_All";
    private const string CacheKey_CapabilityTypes = "CapabilityTypes_All";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(4); // Reference data changes rarely

    public LicenseOptionsRepository(
        AppDbContext db,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<LicenseOptionsRepository> logger,
        IMemoryCache cache)
    {
        _db = db;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Runs a query against a freshly created, isolated DbContext so it can execute concurrently
    /// with other queries on the pooled factory without sharing a single DbContext across threads.
    /// </summary>
    private async Task<T> RunIsolatedAsync<T>(Func<AppDbContext, Task<T>> query, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct);
        return await query(db);
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

        // Genuinely parallel: each query below gets its own DbContext/connection from the pooled
        // factory so they can execute concurrently against the remote SQL Server.
        var licenseTypeId = license.LicenseTypeId;
        var licenseId = license.LicenseId;
        var licenseDistributionMethodId = license.LicenseDistributionMethodId;
        var customerId = license.CustomerId;

        // Cached reference data - avoids a DB round trip per request (license types change rarely).
        var licenseTypesTask = GetLicenseTypesCachedAsync(ct);

        var task2 = RunIsolatedAsync(async db => await db.LicenseParent
            .Where(lp => lp.ChildLicenseId == licenseId)
            .Join(db.License, lp => lp.ParentLicenseId, parent => parent.LicenseId, (lp, parent) => parent.Keycode)
            .FirstOrDefaultAsync(ct), ct);

        var task3 = RunIsolatedAsync(async db => await db.LicenseActiveSeats
            .Where(r => r.LicenseId == licenseId)
            .OrderByDescending(r => r.EndDate)
            .Select(r => (int?)r.ConsumedSeats)
            .FirstOrDefaultAsync(ct), ct);

        var task4 = RunIsolatedAsync(async db => await db.LicenseStorage
            .Where(r => r.LicenseId == licenseId)
            .OrderByDescending(r => r.LicenseStorageId)
            .Select(r => (int?)r.StorageGb)
            .FirstOrDefaultAsync(ct), ct);

        var task5 = RunIsolatedAsync(async db => await (from lal in db.LicenseAttributeLicense
                     join la in db.LicenseAttribute on lal.LicenseAttributeId equals la.LicenseAttributeId
                     join lav in db.LicenseAttributeLicenseValue on lal.LicenseAttributeLicenseValue equals (int?)lav.Value into lavJoin
                     from lav in lavJoin.DefaultIfEmpty()
                     where lal.LicenseId == licenseId
                     orderby lal.LicenseAttributeLicenseId descending
                     select new
                     {
                         la.LicenseAttributeDescription,
                         la.LicenseAttributeTag,
                         lal.LicenseAttributeLicenseValue,
                         LicenseAttributeLicenseValueDescription = lav == null ? null : lav.Description,
                         LicenseAttributeLastModified = (DateTime?)lal.ModifiedDate,
                     }).AsNoTracking().FirstOrDefaultAsync(ct), ct);

        var task6 = RunIsolatedAsync(async db => await (from oil in db.OrderItemLicense
                     join oi in db.OrderItem on oil.OrderItemId equals oi.OrderItemId
                     join p in db.Product on oi.ProductId equals p.ProductId
                     where oil.LicenseId == licenseId && p.ProductTypeId == 2
                     select oil.OrderItemLicenseId).CountAsync(ct), ct);

        var task7 = RunIsolatedAsync(async db => await (from lh in db.LicenseHistory
                     join ldmc in db.LicenseDistributionMethodChannel on lh.LicenseDistributionMethodId equals ldmc.LicenseDistributionMethodId
                     join ch in db.Channel on ldmc.ChannelId equals ch.ChannelId
                     where lh.LicenseId == licenseId
                     orderby lh.HistoryDate
                     select new { ch.ChannelName, ActivationDate = (DateTime?)lh.InsertDate }).AsNoTracking().FirstOrDefaultAsync(ct), ct);

        var task8 = RunIsolatedAsync(async db => await db.LicenseDistributionMethod
            .Where(m => m.LicenseDistributionMethodId == licenseDistributionMethodId)
            .Select(m => m.LicenseDistributionMethodCode)
            .FirstOrDefaultAsync(ct), ct);

        var task9 = RunIsolatedAsync(async db => await db.LicenseNextBillDate
            .Where(n => n.LicenseId == licenseId)
            .OrderByDescending(n => n.LicenseNextBillDateId)
            .Select(n => (DateTime?)n.NextBillDate)
            .FirstOrDefaultAsync(ct), ct);

        var task10 = RunIsolatedAsync(async db => await db.Customer
            .Where(c => c.CustomerId == customerId)
            .Select(c => c.OptIn)
            .FirstOrDefaultAsync(ct), ct);

        var categoryRowsTask = RunIsolatedAsync(async db => await (from lcl in db.LicenseCategoryLicense
                                join lc in db.LicenseCategory on lcl.LicenseCategoryId equals lc.LicenseCategoryId
                                where lcl.LicenseId == licenseId
                                orderby lcl.LicenseCategoryLicenseId descending
                                select new
                                {
                                    lc.LicenseCategoryId,
                                    lc.LicenseCategoryName,
                                    lc.LicenseCategoryDescription,
                                    lc.BaseCapabilityId,
                                    lcl.StartDate,
                                    EndDate = lcl.EndDate
                                }).AsNoTracking().ToListAsync(ct), ct);

        var capabilityByIdTask = RunIsolatedAsync(async db => await (from c in db.LicenseCapability
                                  join t in db.CapabilityType on c.CapabilityTypeId equals t.CapabilityTypeId
                                  where c.LicenseId == licenseId
                                  select new { c.CapabilityId, t.CapabilityTypeDescription })
                                  .AsNoTracking()
                                  .ToDictionaryAsync(x => x.CapabilityId, x => x.CapabilityTypeDescription, ct), ct);

        await Task.WhenAll(
            licenseTypesTask, task2, task3, task4, task5, task6, task7, task8, task9, task10,
            categoryRowsTask, capabilityByIdTask).ConfigureAwait(false);

        // Extract results
        var licenseTypes = await licenseTypesTask;
        licenseTypes.TryGetValue(licenseTypeId, out var fallbackLicenseTypeDescription);
        var fallbackParentKeycode = await task2;
        var fallbackConsumedSeats = await task3;
        var fallbackStorageGb = await task4;
        var fallbackAttribute = await task5;
        var fallbackRenewalCount = await task6;
        var fallbackChannel = await task7;
        var fallbackDistributionCode = await task8;
        var fallbackNextBillDate = await task9;
        var fallbackEmailOptIn = await task10;
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

        // Genuinely parallel: independent of each other, each gets its own isolated context.
        var upgradeTask = primaryCategory is null
            ? Task.FromResult(new List<dynamic>())
            : RunIsolatedAsync(async db =>
                (await (from plcu in db.ProductLicenseCategoryUpgrade
               join baseLc in db.LicenseCategory on plcu.LicenseCategoryId equals baseLc.LicenseCategoryId
               join upgradeLc in db.LicenseCategory on plcu.UpgradeLicenseCategoryId equals upgradeLc.LicenseCategoryId
               join ih in db.ItemHierarchy on plcu.ItemHierarchyId equals (byte?)ih.ItemHierarchyId
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
               }).AsNoTracking().ToListAsync(ct)).Cast<dynamic>().ToList(), ct);

        var seatsTask = RunIsolatedAsync(async db => await db.LicenseSeat
            .Where(ls => ls.LicenseId == licenseId)
            .OrderByDescending(ls => ls.LicenseSeatId)
            .Select(ls => (int?)ls.LicenseSeats)
            .FirstOrDefaultAsync(ct), ct);

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
            // Genuinely parallel: each query gets its own isolated context so they can run
            // concurrently against the remote SQL Server without sharing a single DbContext.
            var productsTask = RunIsolatedAsync(async db => await (from plc in db.ProductLicenseCategory
                               join p in db.Product on plc.ProductId equals p.ProductId
                               join pt in db.ProductType on p.ProductTypeId equals pt.ProductTypeId
                               join lc in db.LicenseCategory on plc.LicenseCategoryId equals lc.LicenseCategoryId
                               where allowedCategoryIds.Contains(plc.LicenseCategoryId)
                                  && (p.ProductTypeId == 1 || p.ProductTypeId == 2)
                               select new
                               {
                                   p.ProductId,
                                   ProductName = p.ProductDescription,
                                   TypeDescription = pt.ProductTypeDescription,
                                   OptionLicenseCategoryId = plc.LicenseCategoryId,
                                   OptionLicenseCategoryName = lc.LicenseCategoryName,
                               }).AsNoTracking().ToListAsync(ct), ct);

            var allYearsTask = RunIsolatedAsync(async db => await db.ProductLicenseCategoryYears
                .Where(py => allowedCategoryIds.Contains(py.LicenseCategoryId))
                .Select(py => new { py.LicenseCategoryId, py.Years })
                .AsNoTracking()
                .ToListAsync(ct), ct);

            var allSeatsTask = RunIsolatedAsync(async db => await db.ProductLicenseCategorySeat
                .Where(ps => allowedCategoryIds.Contains(ps.LicenseCategoryId))
                .Select(ps => new { ps.LicenseCategoryId, ps.Seats })
                .AsNoTracking()
                .ToListAsync(ct), ct);

            var allPricingTask = RunIsolatedAsync(async db => await (from pp in db.ProductPricing
                                 join plc in db.ProductLicenseCategory on pp.ProductId equals plc.ProductId
                                 where allowedCategoryIds.Contains(plc.LicenseCategoryId)
                                 select new { pp.ProductId, pp.RetailPrice })
                                 .AsNoTracking()
                                 .Distinct()
                                 .ToListAsync(ct), ct);

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
