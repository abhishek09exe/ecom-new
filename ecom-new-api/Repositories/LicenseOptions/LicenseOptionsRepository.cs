using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Responses;
using Microsoft.EntityFrameworkCore;

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

    public LicenseOptionsRepository(AppDbContext db, ILogger<LicenseOptionsRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<string?> ResolveKeycodeFromMessageKeyAsync(
        string messageKey, CancellationToken ct = default)
    {
        if (!Guid.TryParse(messageKey, out var guid)) return null;

        return await (
            from lk in _db.LicenseKey
            join l in _db.License on lk.LicenseId equals l.LicenseId
            where lk.Key == guid
            select l.Keycode
        ).FirstOrDefaultAsync(ct);
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
        ).FirstOrDefaultAsync(ct);

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

        var fallbackLicenseTypeDescription = await _db.LicenseType
            .Where(t => t.LicenseTypeId == license.LicenseTypeId)
            .Select(t => t.LicenseTypeDescription)
            .FirstOrDefaultAsync(ct);

        var fallbackParentKeycode = await (
            from lp in _db.LicenseParent
            join parent in _db.License on lp.ParentLicenseId equals parent.LicenseId
            where lp.ChildLicenseId == license.LicenseId
            select parent.Keycode
        ).FirstOrDefaultAsync(ct);

        var fallbackConsumedSeats = await _db.LicenseActiveSeats
            .Where(r => r.LicenseId == license.LicenseId)
            .OrderByDescending(r => r.EndDate)
            .Select(r => (int?)r.ConsumedSeats)
            .FirstOrDefaultAsync(ct);

        var fallbackStorageGb = await _db.LicenseStorage
            .Where(r => r.LicenseId == license.LicenseId)
            .OrderByDescending(r => r.LicenseStorageId)
            .Select(r => (int?)r.StorageGb)
            .FirstOrDefaultAsync(ct);

        var fallbackAttribute = await (
            from lal in _db.LicenseAttributeLicense
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
            }
        ).FirstOrDefaultAsync(ct);

        var fallbackRenewalCount = await (
            from oil in _db.OrderItemLicense
            join oi in _db.OrderItem on oil.OrderItemId equals oi.OrderItemId
            join p in _db.Product on oi.ProductId equals p.ProductId
            where oil.LicenseId == license.LicenseId && p.ProductTypeId == 2
            select oil.OrderItemLicenseId
        ).CountAsync(ct);

        var fallbackChannel = await (
            from lh in _db.LicenseHistory
            join ldmc in _db.LicenseDistributionMethodChannel on lh.LicenseDistributionMethodId equals ldmc.LicenseDistributionMethodId
            join ch in _db.Channel on ldmc.ChannelId equals ch.ChannelId
            where lh.LicenseId == license.LicenseId
            orderby lh.HistoryDate
            select new { ch.ChannelName, ActivationDate = (DateTime?)lh.InsertDate }
        ).FirstOrDefaultAsync(ct);

        var fallbackDistributionCode = await _db.LicenseDistributionMethod
            .Where(m => m.LicenseDistributionMethodId == license.LicenseDistributionMethodId)
            .Select(m => m.LicenseDistributionMethodCode)
            .FirstOrDefaultAsync(ct);

        var fallbackNextBillDate = await _db.LicenseNextBillDate
            .Where(n => n.LicenseId == license.LicenseId)
            .OrderByDescending(n => n.LicenseNextBillDateId)
            .Select(n => (DateTime?)n.NextBillDate)
            .FirstOrDefaultAsync(ct);

        var fallbackEmailOptIn = await _db.Customer
            .Where(c => c.CustomerId == license.CustomerId)
            .Select(c => c.OptIn)
            .FirstOrDefaultAsync(ct);

        // fetch ALL category rows for license_profile; also used for product options (first/most-recent entry)
        var categoryRows = await (
            from lcl in _db.LicenseCategoryLicense
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
            }
        ).ToListAsync(ct);

        var primaryCategory = categoryRows.FirstOrDefault();

        var capabilityById = await (
            from c in _db.LicenseCapability
            join t in _db.CapabilityType on c.CapabilityTypeId equals t.CapabilityTypeId
            where c.LicenseId == license.LicenseId
            select new { c.CapabilityId, t.CapabilityTypeDescription }
        ).ToDictionaryAsync(x => x.CapabilityId, x => x.CapabilityTypeDescription, ct);

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

        var upgradeCategoryRows = primaryCategory is null
            ? []
            : await (
                from plcu in _db.ProductLicenseCategoryUpgrade
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
                }
            )
            .ToListAsync(ct);

        var upgradeCategories = upgradeCategoryRows
            .ToDictionary(
                row => row.UpgradeLicenseCategoryName ?? string.Empty,
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

        var seats = await _db.LicenseSeat
            .Where(ls => ls.LicenseId == license.LicenseId)
            .OrderByDescending(ls => ls.LicenseSeatId)
            .Select(ls => (int?)ls.LicenseSeats)
            .FirstOrDefaultAsync(ct);

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
            var products = await (
                from plc in _db.ProductLicenseCategory
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
                }
            ).ToListAsync(ct);

            if (products.Count > 0)
            {
                var productIds = products.Select(p => p.ProductId).ToList();
                var optionCategoryIds = products.Select(p => p.OptionLicenseCategoryId).Distinct().ToList();

                var allYears = await _db.ProductLicenseCategoryYears
                    .Where(py => optionCategoryIds.Contains(py.LicenseCategoryId))
                    .Select(py => new { py.LicenseCategoryId, py.Years })
                    .ToListAsync(ct);

                var allSeats = await _db.ProductLicenseCategorySeat
                    .Where(ps => optionCategoryIds.Contains(ps.LicenseCategoryId))
                    .Select(ps => new { ps.LicenseCategoryId, ps.Seats })
                    .ToListAsync(ct);

                var allPricing = await _db.ProductPricing
                    .Where(pp => productIds.Contains(pp.ProductId))
                    .Select(pp => new { pp.ProductId, pp.RetailPrice })
                    .ToListAsync(ct);

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
}
