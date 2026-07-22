using ecom_new_api.Models.Requests;
using ecom_new_api.Repositories;

namespace ecom_new_api.Services;

/// <summary>
/// SECTION 2.1: Product Determination Service
/// 
/// Computes start_date and expiration_date for primary products (ItemHierarchyId == 1)
/// based on license status and business rules from usp_cart_insert_cart_order_item.
/// 
/// IMPORTANT: This service:
/// - ONLY handles primary items (ItemHierarchyId == 1)
/// - Updates CartOrderItemContext objects in memory only
/// - Does NOT persist to database
/// - Does NOT handle secondary items, product type determination, upgrades, pricing
/// - Operates on the CartOrderPreparedModel created by PreparationService
/// 
/// SOURCE: Translates SQL CASE expressions from usp_cart_insert_cart_order_item Section 2.1
/// into C# if/else logic for license-based date computation.
/// </summary>
public class ProductDeterminationService
{
    private readonly ICartOrderRepository _repository;
    private readonly ILogger<ProductDeterminationService> _logger;

    public ProductDeterminationService(
        ICartOrderRepository repository,
        ILogger<ProductDeterminationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// SECTION 2.1: Determine primary product start_date and expiration_date
    /// 
    /// For each item in the prepared model with ItemHierarchyId == 1:
    /// - Computes start_date based on license status and user input
    /// - Computes expiration_date based on computed start_date and years purchased
    /// 
    /// Updates CartOrderItemContext objects in memory only (no database persistence).
    /// Returns the modified prepared model for downstream processing.
    /// 
    /// Algorithm:
    /// 1. Iterate over all prepared items
    /// 2. Filter to primary items only (ItemHierarchyId == 1)
    /// 3. For each primary item:
    ///    a. Determine if license exists (by keycode)
    ///    b. Apply start_date business rules (7 CASE branches translated to if/else)
    ///    c. Apply expiration_date business rules (6 CASE branches translated to if/else)
    ///    d. Update the item context with computed dates
    /// 4. Return modified model
    /// 
    /// Database lookups performed:
    /// - None. All data comes from CartOrderPreparedModel (Section 1).
    /// 
    /// TODOs:
    /// - Section 2.2+: Secondary product date determination (DEFERRED)
    /// - Section 2.3+: Product type determination by hierarchy and purchase profile (DEFERRED)
    /// </summary>
    /// <remarks>
    /// This method translates the SQL CASE expression from Section 2.1 exactly:
    /// 
    /// SQL LOGIC (simplified):
    /// ---
    /// SET start_date = CASE
    ///   WHEN l.license_id IS NULL AND i.start_date IS NULL 
    ///     THEN CONVERT(DATE, GETDATE())                        -- No license, no explicit date → today
    ///   WHEN l.license_id IS NOT NULL AND i.start_date IS NULL AND l.category_type_name = 'trial'
    ///     THEN CONVERT(DATE, GETDATE())                        -- Trial license, no explicit date → today
    ///   ... (5 more branches)
    ///   ELSE i.start_date
    /// END
    /// 
    /// C# EQUIVALENT:
    /// ---
    /// var start_date = (license is null && item.StartDate is null)
    ///   ? DateTime.Today
    ///   : (license is not null && item.StartDate is null && license.CategoryType == "trial")
    ///   ? DateTime.Today
    ///   : ... (5 more branches)
    ///   : item.StartDate;
    /// </remarks>
    public async Task<CartOrderPreparedModel> DeterminePrimaryProductDatesAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        // Validation
        if (preparedModel is null)
        {
            _logger.LogWarning("DeterminePrimaryProductDatesAsync called with null prepared model");
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug(
            "Starting Section 2.1: Determining primary product dates for {ItemCount} items",
            preparedModel.Items.Count);

        // Filter to primary items only (ItemHierarchyId == 1)
        var primaryItems = preparedModel.Items
            .Where(item => item.ItemHierarchyId.HasValue && item.ItemHierarchyId.Value == 1)
            .ToList();

        if (primaryItems.Count == 0)
        {
            _logger.LogInformation("No primary items found in prepared model; skipping date determination");
            return preparedModel;
        }

        _logger.LogDebug("Processing {PrimaryItemCount} primary items", primaryItems.Count);

        // Process each primary item
        foreach (var item in primaryItems)
        {
            _logger.LogDebug(
                "Processing primary item: LineItem={LineItem}, ProductId={ProductId}, Category={Category}, Years={Years}",
                item.LineItem, item.ProductId, item.LicenseCategoryName, item.Years);

            // Determine if a license exists for this item
            // A license exists if:
            // 1. PreparedModel has a License (loaded from keycode)
            // 2. The license's keycode matches the item's keycode (typically the same throughout order)
            var licenseExists = preparedModel.License is not null
                && !string.IsNullOrEmpty(preparedModel.License.Keycode);

            _logger.LogDebug("License exists: {LicenseExists}", licenseExists);

            // ════════════════════════════════════════════════════════════════════════════════════
            // SECTION 2.1.A: Compute START_DATE
            // ════════════════════════════════════════════════════════════════════════════════════

            var computedStartDate = ComputePrimaryStartDate(
                item: item,
                licenseExists: licenseExists,
                license: preparedModel.License);

            _logger.LogDebug(
                "Computed start_date: Original={OriginalDate}, Computed={ComputedDate}",
                item.StartDate?.ToString("yyyy-MM-dd") ?? "null",
                computedStartDate?.ToString("yyyy-MM-dd") ?? "null");

            item.StartDate = computedStartDate;

            // ════════════════════════════════════════════════════════════════════════════════════
            // SECTION 2.1.B: Compute EXPIRATION_DATE
            // ════════════════════════════════════════════════════════════════════════════════════

            var computedExpirationDate = ComputePrimaryExpirationDate(
                item: item,
                licenseExists: licenseExists,
                license: preparedModel.License);

            _logger.LogDebug(
                "Computed expiration_date: Original={OriginalDate}, Computed={ComputedDate}",
                item.ExpirationDate?.ToString("yyyy-MM-dd") ?? "null",
                computedExpirationDate?.ToString("yyyy-MM-dd") ?? "null");

            item.ExpirationDate = computedExpirationDate;

            _logger.LogInformation(
                "Section 2.1 complete for item {LineItem}: StartDate={StartDate}, ExpirationDate={ExpirationDate}",
                item.LineItem,
                item.StartDate?.ToString("yyyy-MM-dd") ?? "null",
                item.ExpirationDate?.ToString("yyyy-MM-dd") ?? "null");
        }

        return preparedModel;
    }

    /// <summary>
    /// Computes start_date for a primary product item.
    /// 
    /// Translates SQL Section 2.1 start_date CASE expression into C# if/else logic.
    /// 
    /// BUSINESS RULES:
    /// 1. No license + no explicit date → today
    /// 2. License exists + no explicit date + trial → today
    /// 3. License exists + no explicit date + monthly billing → today
    /// 4. License exists + no explicit date + full category + 0 years → today
    /// 5. License exists + no explicit date + full category + years > 0 + license expired → today
    /// 6. License exists + no explicit date + full category + years > 0 + license current → license expiration date
    /// 7. Otherwise → use provided date
    /// </summary>
    private DateTime? ComputePrimaryStartDate(
        CartOrderItemContext item,
        bool licenseExists,
        CartOrderLicenseContext? license)
    {
        _logger.LogDebug("Computing start_date with rules: LicenseExists={Exists}, Item.StartDate={Date}",
            licenseExists, item.StartDate?.ToString("yyyy-MM-dd") ?? "null");

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 1: No license exists + no explicit start_date provided
        // Condition: l.license_id IS NULL AND i.start_date IS NULL
        // Action: Use today
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (!licenseExists && item.StartDate is null)
        {
            _logger.LogDebug("Rule 1 matched: No license + no explicit date → today");
            return DateTime.Today;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 2: License exists + no explicit start_date + category is 'trial'
        // Condition: l.license_id IS NOT NULL AND i.start_date IS NULL AND l.category_type_name = 'trial'
        // Action: Use today
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (licenseExists && item.StartDate is null && IsTrialLicense(license))
        {
            _logger.LogDebug("Rule 2 matched: Trial license + no explicit date → today");
            return DateTime.Today;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 3: License exists + no explicit start_date + billing model is monthly
        // Condition: l.license_id IS NOT NULL AND i.start_date IS NULL 
        //            AND l.license_attribute_license_value IN (12, 110, 111, 112, 210, 211, 212)
        // Action: Use today
        // 
        // TODO: Get billing model (license_attribute_license_value) from license context
        //       Currently DEFERRED because billing model is available in bundle JSON (Section 1.3),
        //       not in the License entity. May need to load from license profile or add to CartOrderLicenseContext.
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (licenseExists && item.StartDate is null && IsMonthlyBillingModel(license))
        {
            _logger.LogDebug("Rule 3 matched: Monthly billing model + no explicit date → today");
            return DateTime.Today;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 4: License exists + no explicit start_date + full category + 0 years
        // Condition: l.license_id IS NOT NULL AND i.start_date IS NULL 
        //            AND l.category_type_name = 'full' AND i.years = 0
        // Action: Use today
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (licenseExists && item.StartDate is null && IsFullLicense(license)
            && item.Years.HasValue && item.Years.Value == 0)
        {
            _logger.LogDebug("Rule 4 matched: Full license + 0 years → today");
            return DateTime.Today;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 5: License exists + no explicit start_date + full category + years > 0 + license expired/no expiry
        // Condition: l.license_id IS NOT NULL AND i.start_date IS NULL 
        //            AND l.category_type_name = 'full' AND i.years <> 0 
        //            AND (l.expiration_date IS NULL OR l.expiration_date < GETDATE())
        // Action: Use today
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (licenseExists && item.StartDate is null && IsFullLicense(license)
            && item.Years.HasValue && item.Years.Value != 0
            && IsLicenseExpiredOrNoExpiration(license))
        {
            _logger.LogDebug("Rule 5 matched: Full license + years > 0 + expired/no expiration → today");
            return DateTime.Today;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 6: License exists + no explicit start_date + full category + years > 0 + license is current
        // Condition: l.license_id IS NOT NULL AND i.start_date IS NULL 
        //            AND l.category_type_name = 'full' AND i.years <> 0 
        //            AND l.expiration_date >= GETDATE()
        // Action: Use license's expiration date
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (licenseExists && item.StartDate is null && IsFullLicense(license)
            && item.Years.HasValue && item.Years.Value != 0
            && !IsLicenseExpiredOrNoExpiration(license)
            && license?.ExpirationDate is not null)
        {
            _logger.LogDebug("Rule 6 matched: Full license + current → use license expiration date");
            return license.ExpirationDate.Value.Date;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 7 (DEFAULT): Otherwise
        // Action: Use the provided start_date (which may be null)
        // ─────────────────────────────────────────────────────────────────────────────────────────
        _logger.LogDebug("Rule 7 (default) matched: Use provided start_date");
        return item.StartDate;
    }

    /// <summary>
    /// Computes expiration_date for a primary product item.
    /// 
    /// Translates SQL Section 2.1 expiration_date CASE expression into C# if/else logic.
    /// 
    /// BUSINESS RULES:
    /// 1. No license + no explicit date → add (years * 12 months) to today
    /// 2. License exists + no explicit date + trial → add (years * 12 months) to today
    /// 3. License exists + no explicit date + full + no explicit start_date + expired/no expiry 
    ///    → add (years * 12 months) to today
    /// 4. License exists + no explicit date + full + no explicit start_date + current 
    ///    → add (years * 12 months) to license expiration date
    /// 5. License exists + no explicit date + full + explicit start_date 
    ///    → add (years * 12 months) to start_date
    /// 6. Otherwise → use provided date
    /// </summary>
    private DateTime? ComputePrimaryExpirationDate(
        CartOrderItemContext item,
        bool licenseExists,
        CartOrderLicenseContext? license)
    {
        _logger.LogDebug("Computing expiration_date with rules: LicenseExists={Exists}, Item.ExpirationDate={Date}, Years={Years}",
            licenseExists, item.ExpirationDate?.ToString("yyyy-MM-dd") ?? "null", item.Years);

        // Helper: Add months to a date
        DateTime AddMonthsToDate(DateTime date, decimal years)
        {
            var months = (int)(years * 12);
            return date.AddMonths(months);
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 1: No license + no explicit expiration_date provided
        // Condition: l.license_id IS NULL AND i.expiration_date IS NULL
        // Action: Add (years * 12 months) to today
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (!licenseExists && item.ExpirationDate is null && item.Years.HasValue)
        {
            var result = AddMonthsToDate(DateTime.Today, item.Years.Value);
            _logger.LogDebug("Rule 1 matched: No license + no explicit date → today + {Months} months = {Result}",
                item.Years.Value * 12, result.ToString("yyyy-MM-dd"));
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 2: License exists + no explicit expiration_date + category is 'trial'
        // Condition: l.license_id IS NOT NULL AND i.expiration_date IS NULL 
        //            AND l.category_type_name = 'trial'
        // Action: Add (years * 12 months) to today
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (licenseExists && item.ExpirationDate is null && IsTrialLicense(license) && item.Years.HasValue)
        {
            var result = AddMonthsToDate(DateTime.Today, item.Years.Value);
            _logger.LogDebug("Rule 2 matched: Trial license + no explicit date → today + {Months} months = {Result}",
                item.Years.Value * 12, result.ToString("yyyy-MM-dd"));
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 3: License + no explicit expiration_date + full category + no explicit start_date 
        //         + license expired/no expiry
        // Condition: l.license_id IS NOT NULL AND i.expiration_date IS NULL 
        //            AND l.category_type_name = 'full' AND i.start_date IS NULL 
        //            AND (l.expiration_date IS NULL OR l.expiration_date < GETDATE())
        // Action: Add (years * 12 months) to today
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (licenseExists && item.ExpirationDate is null && IsFullLicense(license)
            && item.StartDate is null && IsLicenseExpiredOrNoExpiration(license) && item.Years.HasValue)
        {
            var result = AddMonthsToDate(DateTime.Today, item.Years.Value);
            _logger.LogDebug("Rule 3 matched: Full license + expired + no explicit dates → today + {Months} months = {Result}",
                item.Years.Value * 12, result.ToString("yyyy-MM-dd"));
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 4: License + no explicit expiration_date + full category + no explicit start_date 
        //         + license is current
        // Condition: l.license_id IS NOT NULL AND i.expiration_date IS NULL 
        //            AND l.category_type_name = 'full' AND i.start_date IS NULL 
        //            AND l.expiration_date >= GETDATE()
        // Action: Add (years * 12 months) to license expiration date
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (licenseExists && item.ExpirationDate is null && IsFullLicense(license)
            && item.StartDate is null && !IsLicenseExpiredOrNoExpiration(license)
            && license?.ExpirationDate is not null && item.Years.HasValue)
        {
            var result = AddMonthsToDate(license.ExpirationDate.Value.Date, item.Years.Value);
            _logger.LogDebug("Rule 4 matched: Full license + current + no explicit start → license expiration + {Months} months = {Result}",
                item.Years.Value * 12, result.ToString("yyyy-MM-dd"));
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 5: License + no explicit expiration_date + full category + explicit start_date provided
        // Condition: l.license_id IS NOT NULL AND i.expiration_date IS NULL 
        //            AND l.category_type_name = 'full' AND i.start_date IS NOT NULL
        // Action: Add (years * 12 months) to start_date
        // ─────────────────────────────────────────────────────────────────────────────────────────
        if (licenseExists && item.ExpirationDate is null && IsFullLicense(license)
            && item.StartDate is not null && item.Years.HasValue)
        {
            var result = AddMonthsToDate(item.StartDate.Value.Date, item.Years.Value);
            _logger.LogDebug("Rule 5 matched: Full license + explicit start_date → start_date + {Months} months = {Result}",
                item.Years.Value * 12, result.ToString("yyyy-MM-dd"));
            return result;
        }

        // ─────────────────────────────────────────────────────────────────────────────────────────
        // RULE 6 (DEFAULT): Otherwise
        // Action: Use the provided expiration_date (which may be null)
        // ─────────────────────────────────────────────────────────────────────────────────────────
        _logger.LogDebug("Rule 6 (default) matched: Use provided expiration_date");
        return item.ExpirationDate;
    }

    /// <summary>
    /// Determines if a license is a trial license.
    /// 
    /// Checks if license.CategoryTypeName == "trial"
    /// </summary>
    private bool IsTrialLicense(CartOrderLicenseContext? license)
    {
        if (license is null)
            return false;

        return license.CategoryTypeName == "trial";
    }

    /// <summary>
    /// Determines if a license has a monthly billing model.
    /// 
    /// Monthly billing model codes: 12, 110, 111, 112, 210, 211, 212
    /// </summary>
    private bool IsMonthlyBillingModel(CartOrderLicenseContext? license)
    {
        if (license is null)
            return false;

        var monthlyModels = new[] { 12, 110, 111, 112, 210, 211, 212 };
        return monthlyModels.Contains(license.LicenseAttributeLicenseValue ?? 0);
    }

    /// <summary>
    /// Determines if a license is a full (non-trial) license.
    /// 
    /// Inverse of IsTrialLicense.
    /// </summary>
    private bool IsFullLicense(CartOrderLicenseContext? license)
    {
        if (license is null)
            return false;

        return license.CategoryTypeName == "full";
    }

    /// <summary>
    /// Determines if a license is expired or has no expiration date.
    /// 
    /// Returns true if:
    /// - License expiration date is null (no expiration)
    /// - License expiration date is before today (expired)
    /// 
    /// Returns false if:
    /// - License expiration date is today or later (current/active)
    /// </summary>
    private bool IsLicenseExpiredOrNoExpiration(CartOrderLicenseContext? license)
    {
        if (license?.ExpirationDate is null)
        {
            _logger.LogDebug("License has no expiration date (considered expired for purposes of this rule)");
            return true;
        }

        var isExpired = license.ExpirationDate.Value.Date < DateTime.Today;
        _logger.LogDebug("License expiration: {ExpirationDate}, IsExpired: {IsExpired}",
            license.ExpirationDate.Value.Date.ToString("yyyy-MM-dd"), isExpired);

        return isExpired;
    }

    /// <summary>
    /// SECTION 2.1.1: Determine secondary product start_date and expiration_date
    /// 
    /// For items with ItemHierarchyId == 2 (secondary/add-on products), computes dates based on:
    /// - Primary product dates (i2)
    /// - Existing items in cart (e)
    /// - License information (l)
    /// - Billing model changes
    /// 
    /// Updates CartOrderItemContext objects in memory only (no database persistence).
    /// </summary>
    public async Task<CartOrderPreparedModel> DetermineSecondaryProductDatesAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.1.1: Determining secondary product dates");

        // Filter to secondary items only (ItemHierarchyId == 2)
        var secondaryItems = preparedModel.Items
            .Where(item => item.ItemHierarchyId.HasValue && item.ItemHierarchyId.Value == 2)
            .ToList();

        if (secondaryItems.Count == 0)
        {
            _logger.LogInformation("No secondary items found; skipping secondary date determination");
            return preparedModel;
        }

        _logger.LogDebug("Processing {SecondaryItemCount} secondary items", secondaryItems.Count);

        foreach (var secondaryItem in secondaryItems)
        {
            // ISSUE #7 FIX: Use CartItemBundleId to find matching primary item
            // SQL: LEFT JOIN @item_table i2 ON i2.cart_item_bundle_id = i.cart_item_bundle_id AND i2.item_hierarchy_id = 1
            var bundleId = secondaryItem.CartItemBundleId;
            var primaryItem = preparedModel.Items.FirstOrDefault(i =>
                i.ItemHierarchyId == 1 && i.CartItemBundleId == bundleId);

            _logger.LogDebug("Processing secondary item {LineItem}: BundleId={BundleId}, HasPrimaryMatch={HasPrimary}", 
                secondaryItem.LineItem, bundleId, primaryItem is not null);

            // Compute start_date for secondary
            secondaryItem.StartDate = ComputeSecondaryStartDate(
                secondaryItem: secondaryItem,
                primaryItem: primaryItem,
                license: preparedModel.License,
                existingItems: preparedModel.ExistingItems,
                bundleId: bundleId);

            // Compute expiration_date for secondary
            secondaryItem.ExpirationDate = ComputeSecondaryExpirationDate(
                secondaryItem: secondaryItem,
                primaryItem: primaryItem,
                license: preparedModel.License,
                preparedModel: preparedModel);

            _logger.LogInformation("Section 2.1.1 complete for secondary item {LineItem}: StartDate={StartDate}, ExpirationDate={ExpirationDate}",
                secondaryItem.LineItem,
                secondaryItem.StartDate?.ToString("yyyy-MM-dd") ?? "null",
                secondaryItem.ExpirationDate?.ToString("yyyy-MM-dd") ?? "null");
        }

        return preparedModel;
    }

    /// <summary>
    /// Computes start_date for a secondary product item (ItemHierarchyId == 2).
    /// 
    /// Translates SQL Section 2.1.1 start_date CASE expression.
    /// </summary>
    private DateTime? ComputeSecondaryStartDate(
        CartOrderItemContext secondaryItem,
        CartOrderItemContext? primaryItem,
        CartOrderLicenseContext? license,
        List<CartOrderItemContext>? existingItems = null,
        int? bundleId = null)
    {
        _logger.LogDebug("Computing secondary start_date");

        // RULE 1: Billing model switched → use primary start_date
        // SQL: ISNULL(l.license_attribute_license_value, @license_attribute_license_value) <> @license_attribute_license_value
        if (license is not null && primaryItem is not null)
        {
            var licenseModel = license.LicenseAttributeLicenseValue ?? 0;
            var bundleModel = license.LicenseAttributeLicenseValueFromLicense ?? 0;
            if (licenseModel != bundleModel)
            {
                _logger.LogDebug("Secondary Rule 1: Billing model switched ({LicenseModel} != {BundleModel}) → use primary start_date",
                    licenseModel, bundleModel);
                return primaryItem.StartDate;
            }
        }

        // RULE 2: Primary exists but secondary doesn't + secondary has explicit date → use explicit
        if (primaryItem?.StartDate is not null && secondaryItem.StartDate is not null)
        {
            _logger.LogDebug("Secondary Rule 2: Pure upsell → use explicit start_date");
            return secondaryItem.StartDate;
        }

        // RULE 3: Primary empty + secondary 0 years → today
        if (primaryItem?.StartDate is null && secondaryItem.Years.HasValue && secondaryItem.Years.Value == 0)
        {
            _logger.LogDebug("Secondary Rule 3: No primary + 0 years → today");
            return DateTime.Today;
        }

        // RULE 4: Primary empty + trial + years > 0 → today
        if (primaryItem?.StartDate is null && IsTrialLicense(license) && 
            secondaryItem.Years.HasValue && secondaryItem.Years.Value > 0)
        {
            _logger.LogDebug("Secondary Rule 4: Trial + years > 0 → today");
            return DateTime.Today;
        }

        // RULE 5: Primary empty + years > 0 → license expiration or today
        if (primaryItem?.StartDate is null && secondaryItem.Years.HasValue && secondaryItem.Years.Value > 0)
        {
            var result = license?.ExpirationDate ?? DateTime.Today;
            _logger.LogDebug("Secondary Rule 5: No primary + years > 0 → {Result}",
                result.Date.ToString("yyyy-MM-dd"));
            return result.Date;
        }

        // RULE 6: No new primary item, but existing primary item in cart with same bundle → use existing start_date
        // SQL: WHEN i2.start_date IS NULL AND e.start_date IS NOT NULL THEN e.start_date
        // LEFT JOIN @existing_item_table e ON e.cart_item_bundle_id = i.cart_item_bundle_id AND e.item_hierarchy_id = 1
        if (primaryItem?.StartDate is null && bundleId.HasValue)
        {
            var existingPrimary = existingItems?.FirstOrDefault(e =>
                e.ItemHierarchyId == 1 && e.CartItemBundleId == bundleId.Value);

            if (existingPrimary?.StartDate is not null)
            {
                _logger.LogDebug(
                    "Secondary Rule 6: No new primary + existing primary in cart for bundle {BundleId} → use existing start_date {Date}",
                    bundleId, existingPrimary.StartDate.Value.ToString("yyyy-MM-dd"));
                return existingPrimary.StartDate;
            }

            _logger.LogDebug("Secondary Rule 6: No new primary and no existing primary for bundle {BundleId}", bundleId);
        }

        // RULE 7: Primary exists + trial + years > 0 → today
        if (primaryItem?.StartDate is not null && IsTrialLicense(license) &&
            secondaryItem.Years.HasValue && secondaryItem.Years.Value > 0)
        {
            _logger.LogDebug("Secondary Rule 7: Trial + years > 0 + primary exists → today");
            return DateTime.Today;
        }

        // RULE 8 (ELSE/DEFAULT): Complex COALESCE logic
        if (secondaryItem.Years.HasValue && secondaryItem.Years.Value == 0 && primaryItem?.StartDate is not null)
        {
            _logger.LogDebug("Secondary Rule 8a: 0 years → use primary start_date");
            return primaryItem.StartDate;
        }

        if (license?.ExpirationDate is not null && license.ExpirationDate.Value.Date < DateTime.Today && 
            primaryItem?.StartDate is not null)
        {
            _logger.LogDebug("Secondary Rule 8b: Expired license → today (not earlier than today)");
            return DateTime.Today;
        }

        if (license?.ExpirationDate is not null && license.ExpirationDate.Value.Date >= DateTime.Today &&
            primaryItem?.StartDate is not null)
        {
            _logger.LogDebug("Secondary Rule 8c: Current license → use license expiration");
            return license.ExpirationDate.Value.Date;
        }

        _logger.LogDebug("Secondary Rule 8 (default): Use primary start_date");
        return primaryItem?.StartDate;
    }

    /// <summary>
    /// Computes expiration_date for a secondary product item (ItemHierarchyId == 2).
    /// 
    /// Translates SQL Section 2.1.1 expiration_date CASE expression with LEFT JOINs to:
    /// - i2: Primary product in same bundle
    /// - e: Existing secondary item already in cart
    /// - x: Max expiration date aggregated from license table
    /// </summary>
    private DateTime? ComputeSecondaryExpirationDate(
        CartOrderItemContext secondaryItem,
        CartOrderItemContext? primaryItem,
        CartOrderLicenseContext? license,
        CartOrderPreparedModel? preparedModel = null)
    {
        _logger.LogDebug("Computing secondary expiration_date");

        // RULE 1: Pure upsell - Secondary has explicit expiration but primary doesn't exist
        // SQL: WHEN i2.expiration_date IS NULL AND i.expiration_date IS NOT NULL THEN i.expiration_date
        if (primaryItem?.ExpirationDate is null && secondaryItem.ExpirationDate is not null)
        {
            _logger.LogDebug("Secondary expiration Rule 1: Pure upsell → use explicit expiration_date");
            return secondaryItem.ExpirationDate;
        }

        // RULE 2: Primary product exists - use its expiration date
        // SQL: WHEN i2.expiration_date IS NOT NULL THEN i2.expiration_date
        if (primaryItem?.ExpirationDate is not null)
        {
            _logger.LogDebug("Secondary expiration Rule 2: Primary exists → use primary expiration_date");
            return primaryItem.ExpirationDate;
        }

        // RULE 3: Existing secondary in cart - use its expiration date
        // SQL: WHEN e.expiration_date IS NOT NULL THEN e.expiration_date
        // Note: This would require loading existing item from database
        // For now, we check if secondary already has expiration from loaded data
        if (secondaryItem.ExpirationDate is not null)
        {
            _logger.LogDebug("Secondary expiration Rule 3: Use existing secondary expiration_date");
            return secondaryItem.ExpirationDate;
        }

        // RULE 4: Upgrade of secondary only - use max license expiration
        // SQL: WHEN i2.expiration_date IS NULL THEN x.expiration_date (max(expiration_date) grouped by license_id)
        if (license?.ExpirationDate is not null)
        {
            _logger.LogDebug("Secondary expiration Rule 4: No primary → use license max expiration_date");
            return license.ExpirationDate.Value.Date;
        }

        _logger.LogDebug("Secondary expiration (default): No dates available");
        return null;
    }

    /// <summary>
    /// SECTION 2.2: Determine primary product_type
    /// 
    /// Computes product_type_id for primary items (ItemHierarchyId == 1):
    /// - 1 = New (no license)
    /// - 2 = Renewal (license exists, no change)
    /// - 3 = Upgrade (license exists, expanded capacity)
    /// 
    /// Updates CartOrderItemContext.ProductTypeId in memory only.
    /// </summary>
    public async Task<CartOrderPreparedModel> DeterminePrimaryProductTypeAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.2: Determining primary product types");

        var primaryItems = preparedModel.Items
            .Where(item => item.ItemHierarchyId.HasValue && item.ItemHierarchyId.Value == 1 &&
                          (!item.ProductTypeId.HasValue || item.ProductTypeId.Value == 0))
            .ToList();

        if (primaryItems.Count == 0)
        {
            _logger.LogInformation("No primary items without product_type; skipping product type determination");
            return preparedModel;
        }

        _logger.LogDebug("Processing {PrimaryItemCount} primary items for type determination", primaryItems.Count);

        foreach (var item in primaryItems)
        {
            var productType = ComputePrimaryProductType(item, preparedModel);
            _logger.LogDebug("Computed product_type_id: {ItemLineItem} → {ProductType}",
                item.LineItem, productType);
            item.ProductTypeId = productType;

            _logger.LogInformation("Section 2.2 complete for item {LineItem}: ProductTypeId={ProductType}",
                item.LineItem, productType);
        }

        return preparedModel;
    }

    /// <summary>
    /// Computes product_type_id for a primary item.
    /// 
    /// Translates SQL Section 2.2 product_type CASE expression.
    /// Returns: 1 (New), 2 (Renewal), 3 (Upgrade)
    /// </summary>
    private int ComputePrimaryProductType(
        CartOrderItemContext item,
        CartOrderPreparedModel preparedModel)
    {
        var license = preparedModel.License;

        // RULE 1: No license → 1 (New)
        if (license is null)
        {
            _logger.LogDebug("Product Type Rule 1: No license → type 1 (New)");
            return 1;
        }

        // RULE 2: License is trial → 1 (Trial conversion = New)
        if (license.CategoryTypeName == "trial")
        {
            _logger.LogDebug("Product Type Rule 2: Trial license → type 1 (Trial conversion)");
            return 1;
        }

        // RULE 3: Full license + item expiration > license expiration → 2 (Renewal)
        if (license.CategoryTypeName == "full" && item.ExpirationDate > license.ExpirationDate)
        {
            _logger.LogDebug("Product Type Rule 3: Expiration extended → type 2 (Renewal)");
            return 2;
        }

        // RULE 4: WIFI + date-based upgrade check → 2 (Renewal)
        if (license.CategoryTypeName == "full" && license.LicenseCategoryName == "WIFI" &&
            item.ExpirationDate?.Date > license.ExpirationDate?.Date)
        {
            _logger.LogDebug("Product Type Rule 4: WIFI expiration extended → type 2 (Renewal)");
            return 2;
        }

        // RULE 5: WIFI + seat/year upgrade → 3 (Upgrade)
        if (license.CategoryTypeName == "full" && license.LicenseCategoryName == "WIFI" &&
            (item.LicenseSeats > license.LicenseSeats ||
             item.Years > license.AutorenewCycle))
        {
            _logger.LogDebug("Product Type Rule 5: WIFI seats/years expanded → type 3 (Upgrade)");
            return 3;
        }

        // RULE 6: Full + same expiration + category/seats changed → 3 (Upgrade)
        if (license.CategoryTypeName == "full" && item.ExpirationDate == license.ExpirationDate &&
            (item.LicenseCategoryName != license.LicenseCategoryName ||
             item.TotalLicenseSeats > license.LicenseSeats))
        {
            _logger.LogDebug("Product Type Rule 6: Category/seats changed at same expiration → type 3 (Upgrade)");
            return 3;
        }

        // DEFAULT: All other cases → 2 (Renewal)
        _logger.LogDebug("Product Type (default): No specific rule matched → type 2 (Renewal)");
        return 2;
    }

    /// <summary>
    /// SECTION 2.2.1: Create upgrade items
    /// 
    /// For primary items marked as upgrades (product_type_id == 3), creates additional
    /// CartOrderItemContext objects representing the quantity/capacity upgrade.
    /// 
    /// Creates in-memory only (no database persistence).
    /// Two different INSERT logic branches based on product_line_id.
    /// </summary>
    public async Task<CartOrderPreparedModel> DetermineUpgradeItemsAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.2.1: Determining upgrade items");

        var primaryUpgrades = preparedModel.Items
            .Where(item => item.ItemHierarchyId == 1 && item.ProductTypeId == 3)
            .ToList();

        if (primaryUpgrades.Count == 0)
        {
            _logger.LogInformation("No primary upgrade items found; skipping upgrade item creation");
            return preparedModel;
        }

        _logger.LogDebug("Found {UpgradeCount} primary items marked for upgrade", primaryUpgrades.Count);

        var license = preparedModel.License;
        if (license is null)
        {
            _logger.LogWarning("No license available; cannot create upgrade items");
            return preparedModel;
        }

        var siteId = preparedModel.SiteId ?? string.Empty;
        var productLineId = preparedModel.ProductLineId;
        var billingModelId = preparedModel.BillingModelId;
        var itemIdCounter = preparedModel.Items.Count + 1;

        foreach (var primaryUpgrade in primaryUpgrades)
        {
            // WHERE conditions common to both branches (lines 1200-1212 SQL)
            // Check: (category changed OR seats expanded)
            var categoryChanged = primaryUpgrade.LicenseCategoryName != license.LicenseCategoryName;
            var seatsExpanded = primaryUpgrade.LicenseSeats > license.LicenseSeats;
            if (!categoryChanged && !seatsExpanded)
            {
                _logger.LogDebug("Section 2.2.1: Item {LineItem} skipped (no category/seat change)", primaryUpgrade.LineItem);
                continue;
            }

            // Check: license not expired (l.expiration_date > GETDATE())
            if (license.ExpirationDate?.Date <= DateTime.Today)
            {
                _logger.LogDebug("Section 2.2.1: Item {LineItem} skipped (license expired)", primaryUpgrade.LineItem);
                continue;
            }

            // RULE 1: Product line is 100 or 200 (Partner path - WIFI upgrade)
            if (productLineId is 100 or 200)
            {
                _logger.LogDebug("Section 2.2.1 Branch 1 (Partner): Checking WIFI upgrade item");
                
                // Partner branch: Only WIFI (line 1211: i.license_category_name != 'WIFI' excludes non-WIFI)
                if (primaryUpgrade.LicenseCategoryName == "WIFI")
                {
                    var wifiUpgradeItem = new CartOrderItemContext
                    {
                        CartOrderItemId = itemIdCounter++,
                        CartOrderId = primaryUpgrade.CartOrderId,
                        LineItem = primaryUpgrade.LineItem + 1,
                        ProductId = primaryUpgrade.ProductId,
                        LicenseCategoryName = primaryUpgrade.LicenseCategoryName,
                        Quantity = primaryUpgrade.Quantity,
                        LicenseSeats = license.LicenseSeats,
                        TotalLicenseSeats = primaryUpgrade.TotalLicenseSeats,
                        StorageGb = primaryUpgrade.StorageGb,
                        Years = 0,
                        LicenseKeycodeTypeId = primaryUpgrade.LicenseKeycodeTypeId ?? 0,
                        StartDate = DateTime.Today,
                        ExpirationDate = license.ExpirationDate?.Date,
                        ItemHierarchyId = 1,
                        ProductTypeId = 3,
                        LoadedAt = DateTime.UtcNow
                    };

                    preparedModel.Items.Add(wifiUpgradeItem);
                    _logger.LogInformation("Section 2.2.1: Created WIFI upgrade item {ItemId}", itemIdCounter - 1);
                }
                else
                {
                    _logger.LogDebug("Section 2.2.1: Item {LineItem} skipped (partner branch requires WIFI)", primaryUpgrade.LineItem);
                }
            }
            // RULE 2: Standard branch (non-partner, non-SFDC)
            else if (siteId != "SFDC")
            {
                _logger.LogDebug("Section 2.2.1 Branch 2 (Standard): Checking standard upgrade item");

                // Check: Carbonite exclusion (clc.license_category_name IS null)
                // Carbonite categories: those with usage_pricing_model_id = 2 (capacity)
                var carboniteCategories = new[] { "Carbonite", "CarboniteOverage" };
                if (carboniteCategories.Contains(primaryUpgrade.LicenseCategoryName))
                {
                    _logger.LogDebug("Section 2.2.1: Item {LineItem} skipped (Carbonite category excluded)", primaryUpgrade.LineItem);
                    continue;
                }

                // Check: Billing model restrictions
                // Global: @license_attribute_license_value NOT IN (12, 110, 111, 112, 210, 211, 212, 13, 213, 113)
                // License: l.license_attribute_license_value NOT IN (12, 110, 111, 112, 210, 211, 212)
                var blockedGlobalModels = new[] { 12, 110, 111, 112, 210, 211, 212, 13, 213, 113 };
                var blockedLicenseModels = new[] { 12, 110, 111, 112, 210, 211, 212 };

                var globalBillingModel = preparedModel.GlobalBillingModelId ?? 0;
                var licenseBillingModel = license.LicenseAttributeLicenseValue ?? 0;

                if (blockedGlobalModels.Contains(globalBillingModel) || blockedLicenseModels.Contains(licenseBillingModel))
                {
                    _logger.LogDebug("Section 2.2.1: Item {LineItem} skipped (billing model restricted: global={Global}, license={License})",
                        primaryUpgrade.LineItem, globalBillingModel, licenseBillingModel);
                    continue;
                }

                var upgradeItem = new CartOrderItemContext
                {
                    CartOrderItemId = itemIdCounter++,
                    CartOrderId = primaryUpgrade.CartOrderId,
                    LineItem = primaryUpgrade.LineItem + 1,
                    ProductId = primaryUpgrade.ProductId,
                    LicenseCategoryName = primaryUpgrade.LicenseCategoryName,
                    Quantity = (primaryUpgrade.LicenseSeats ?? 0) - (license.LicenseSeats ?? 0),
                    LicenseSeats = (primaryUpgrade.LicenseSeats ?? 0) - (license.LicenseSeats ?? 0),
                    TotalLicenseSeats = primaryUpgrade.TotalLicenseSeats,
                    StorageGb = primaryUpgrade.StorageGb,
                    Years = 0,
                    LicenseKeycodeTypeId = primaryUpgrade.LicenseKeycodeTypeId ?? 0,
                    StartDate = DateTime.Today,
                    ExpirationDate = license.ExpirationDate?.Date,
                    ItemHierarchyId = 1,
                    ProductTypeId = 3,
                    UsagePricingModelId = primaryUpgrade.UsagePricingModelId,
                    RetentionModelId = primaryUpgrade.RetentionModelId,
                    RetentionTerm = primaryUpgrade.RetentionTerm,
                    ProductPlatformId = primaryUpgrade.ProductPlatformId,
                    LoadedAt = DateTime.UtcNow
                };

                preparedModel.Items.Add(upgradeItem);
                _logger.LogInformation("Section 2.2.1: Created standard upgrade item {ItemId}", itemIdCounter - 1);
            }
            else
            {
                _logger.LogDebug("Section 2.2.1: Item {LineItem} skipped (SFDC order)", primaryUpgrade.LineItem);
            }
        }

        _logger.LogInformation("Section 2.2.1 complete: {UpgradeItemCount} upgrade items created",
            itemIdCounter - (primaryUpgrades.Count + 1));

        return preparedModel;
    }

    /// <summary>
    /// SECTION 2.2.2: Retention model upgrade detection
    /// 
    /// For primary items with retention_term > license retention_term (SFDC only):
    /// - Sets ProductTypeId = 3 (Upgrade)
    /// - Sets Years = 0
    /// 
    /// Translates SQL UPDATE from Section 2.2.2 exactly.
    /// </summary>
    public async Task<CartOrderPreparedModel> DetermineRetentionModelUpgradeAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.2.2: Determining retention model upgrades");

        var siteId = preparedModel.SiteId ?? string.Empty;
        if (siteId != "SFDC")
        {
            _logger.LogDebug("Section 2.2.2: Not SFDC order; skipping retention model upgrade check");
            return preparedModel;
        }

        var license = preparedModel.License;
        if (license?.RetentionTerm is null)
        {
            _logger.LogDebug("Section 2.2.2: No license retention term; skipping check");
            return preparedModel;
        }

        var primaryItems = preparedModel.Items
            .Where(item => item.ItemHierarchyId == 1 &&
                          item.ProductTypeId == 2 &&
                          item.RetentionTerm.HasValue &&
                          item.RetentionTerm.Value > license.RetentionTerm.Value)
            .ToList();

        if (primaryItems.Count == 0)
        {
            _logger.LogInformation("Section 2.2.2: No retention model upgrades detected");
            return preparedModel;
        }

        _logger.LogDebug("Found {UpgradeCount} retention model upgrades", primaryItems.Count);

        foreach (var item in primaryItems)
        {
            _logger.LogDebug("Section 2.2.2: Item {LineItem} retention_term {ItemRetention} > license {LicenseRetention}",
                item.LineItem, item.RetentionTerm, license.RetentionTerm);
            
            item.ProductTypeId = 3;
            item.Years = 0;

            _logger.LogInformation("Section 2.2.2: Item {LineItem} updated: ProductTypeId=3, Years=0",
                item.LineItem);
        }

        return preparedModel;
    }

    /// <summary>
    /// SECTION 2.3: Determine secondary product_type
    /// 
    /// For items with ItemHierarchyId == 2 and no ProductTypeId, computes type:
    /// - 1 = New (no license)
    /// - 2 = Renewal (license exists with same category, no expansion)
    /// - 3 = Upgrade (license exists with same category, seat expansion)
    /// 
    /// Translates SQL Section 2.3 CASE expression exactly.
    /// </summary>
    public async Task<CartOrderPreparedModel> DetermineSecondaryProductTypeAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.3: Determining secondary product types");

        var secondaryItems = preparedModel.Items
            .Where(item => item.ItemHierarchyId == 2 && (!item.ProductTypeId.HasValue || item.ProductTypeId.Value == 0))
            .ToList();

        if (secondaryItems.Count == 0)
        {
            _logger.LogInformation("No secondary items without product_type; skipping");
            return preparedModel;
        }

        _logger.LogDebug("Processing {SecondaryCount} secondary items for type determination", secondaryItems.Count);

        var license = preparedModel.License;

        foreach (var item in secondaryItems)
        {
            var productType = ComputeSecondaryProductType(item, license);
            _logger.LogDebug("Secondary item {LineItem}: ProductTypeId={Type}", item.LineItem, productType);
            item.ProductTypeId = productType;

            _logger.LogInformation("Section 2.3 complete for item {LineItem}: ProductTypeId={Type}",
                item.LineItem, productType);
        }

        return preparedModel;
    }

    /// <summary>
    /// Computes product_type_id for a secondary item.
    /// 
    /// Translates SQL Section 2.3 CASE expression:
    /// - No license → 1 (New)
    /// - Trial license → 1 (Trial conversion)
    /// - Full license + item expiration > license expiration → 2 (Renewal)
    /// - Full license + same expiration + seats expanded → 3 (Upgrade)
    /// - Default → 2 (Renewal)
    /// </summary>
    private int ComputeSecondaryProductType(
        CartOrderItemContext item,
        CartOrderLicenseContext? license)
    {
        // RULE 1: No license → 1 (New)
        if (license is null)
        {
            _logger.LogDebug("Secondary type Rule 1: No license → type 1 (New)");
            return 1;
        }

        // RULE 2: Trial license → 1 (Trial conversion)
        if (license.CategoryTypeName == "trial")
        {
            _logger.LogDebug("Secondary type Rule 2: Trial → type 1 (Trial conversion)");
            return 1;
        }

        // RULE 3: Full license + item expiration > license expiration → 2 (Renewal)
        if (license.CategoryTypeName == "full" && item.ExpirationDate > license.ExpirationDate)
        {
            _logger.LogDebug("Secondary type Rule 3: Expiration extended → type 2 (Renewal)");
            return 2;
        }

        // RULE 4: Full license + same expiration + seats expanded → 3 (Upgrade)
        if (license.CategoryTypeName == "full" && item.ExpirationDate == license.ExpirationDate &&
            item.TotalLicenseSeats > license.LicenseSeats)
        {
            _logger.LogDebug("Secondary type Rule 4: Same expiration + seats expanded → type 3 (Upgrade)");
            return 3;
        }

        // DEFAULT: 2 (Renewal)
        _logger.LogDebug("Secondary type (default) → type 2 (Renewal)");
        return 2;
    }

    /// <summary>
    /// SECTION 2.3.1: Create secondary upgrade items
    /// 
    /// For secondary items with ProductTypeId == 2 (renewals) that have seat expansion
    /// or category change, creates new CartOrderItemContext objects for the upgraded capacity.
    /// 
    /// WHERE conditions (lines 1267-1284 SQL):
    /// - ItemHierarchyId == 2 AND ProductTypeId == 2
    /// - TotalLicenseSeats > license.LicenseSeats
    /// - Billing model restrictions both global and per-license
    /// - Carbonite exclusion (clc.license_category_name IS null)
    /// - Not SFDC
    /// 
    /// Creates in-memory only (no database persistence).
    /// Translates SQL INSERT exactly.
    /// </summary>
    public async Task<CartOrderPreparedModel> CreateSecondaryUpgradeItemsAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.3.1: Creating secondary upgrade items");

        var license = preparedModel.License;
        var siteId = preparedModel.SiteId ?? string.Empty;

        if (siteId == "SFDC")
        {
            _logger.LogDebug("Section 2.3.1: SFDC order; skipping secondary upgrade creation");
            return preparedModel;
        }

        var secondaryRenewals = preparedModel.Items
            .Where(item => item.ItemHierarchyId == 2 &&
                          item.ProductTypeId == 2 &&
                          item.TotalLicenseSeats > (license?.LicenseSeats ?? 0))
            .ToList();

        if (secondaryRenewals.Count == 0)
        {
            _logger.LogInformation("No secondary renewal items with seat expansion; skipping");
            return preparedModel;
        }

        _logger.LogDebug("Found {UpgradeCount} secondary renewals eligible for upgrade", secondaryRenewals.Count);

        if (license is null)
        {
            _logger.LogWarning("No license available; cannot create secondary upgrade items");
            return preparedModel;
        }

        var globalBillingModel = preparedModel.GlobalBillingModelId ?? 0;
        var blockedGlobalModels = new[] { 12, 110, 111, 112, 210, 211, 212, 13, 113, 213 };
        var blockedLicenseModels = new[] { 12, 110, 111, 112, 210, 211, 212, 13, 213, 113 };

        var itemIdCounter = preparedModel.Items.Count + 1;

        foreach (var secondaryRenewal in secondaryRenewals)
        {
            // WHERE Condition: Billing models
            // SQL: @license_attribute_license_value NOT IN (12,110,111,112,210,211,212,13,113,213)
            //      AND l.license_attribute_license_value NOT IN (12,110,111,112,210,211,212,13,213,113)
            if (blockedGlobalModels.Contains(globalBillingModel))
            {
                _logger.LogDebug("Section 2.3.1: Item {LineItem} skipped (global billing model {Model} blocked)",
                    secondaryRenewal.LineItem, globalBillingModel);
                continue;
            }

            var licenseBillingModel = license.LicenseAttributeLicenseValue ?? 0;
            if (blockedLicenseModels.Contains(licenseBillingModel))
            {
                _logger.LogDebug("Section 2.3.1: Item {LineItem} skipped (license billing model {Model} blocked)",
                    secondaryRenewal.LineItem, licenseBillingModel);
                continue;
            }

            // WHERE Condition: Carbonite exclusion (clc.license_category_name IS null)
            // Only create upgrades for non-Carbonite categories
            var carboniteCategories = new[] { "Carbonite", "CarboniteOverage" };
            if (carboniteCategories.Contains(secondaryRenewal.LicenseCategoryName))
            {
                _logger.LogDebug("Section 2.3.1: Item {LineItem} skipped (Carbonite category excluded)",
                    secondaryRenewal.LineItem);
                continue;
            }

            _logger.LogDebug("Section 2.3.1: Creating upgrade for secondary item {LineItem}",
                secondaryRenewal.LineItem);

            var upgradeItem = new CartOrderItemContext
            {
                CartOrderItemId = itemIdCounter++,
                CartOrderId = secondaryRenewal.CartOrderId,
                LineItem = secondaryRenewal.LineItem + 1,
                ProductId = secondaryRenewal.ProductId,
                LicenseCategoryName = secondaryRenewal.LicenseCategoryName,
                Quantity = (secondaryRenewal.LicenseSeats ?? 0) - (license.LicenseSeats ?? 0),
                LicenseSeats = (secondaryRenewal.LicenseSeats ?? 0) - (license.LicenseSeats ?? 0),
                TotalLicenseSeats = secondaryRenewal.LicenseSeats,
                StorageGb = secondaryRenewal.StorageGb,
                Years = 0,
                LicenseKeycodeTypeId = secondaryRenewal.LicenseKeycodeTypeId,
                StartDate = DateTime.Today,
                ExpirationDate = license.ExpirationDate?.Date,
                ItemHierarchyId = 2,
                ProductTypeId = 3,
                UsagePricingModelId = secondaryRenewal.UsagePricingModelId,
                RetentionModelId = secondaryRenewal.RetentionModelId,
                RetentionTerm = secondaryRenewal.RetentionTerm,
                ProductPlatformId = secondaryRenewal.ProductPlatformId,
                LoadedAt = DateTime.UtcNow
            };

            preparedModel.Items.Add(upgradeItem);
            _logger.LogInformation("Section 2.3.1: Created secondary upgrade item {ItemId}", itemIdCounter - 1);
        }

        return preparedModel;
    }

    /// <summary>
    /// SECTION 2.3.2: Default years for secondary new/renewals
    /// 
    /// For secondary items with ProductTypeId IN (1, 2) and Years == 0,
    /// sets Years = 1.
    /// 
    /// Translates SQL UPDATE from Section 2.3.2 exactly.
    /// </summary>
    public async Task<CartOrderPreparedModel> DetermineSecondaryDefaultYearsAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.3.2: Setting default years for secondary items");

        var secondaryNewRenewals = preparedModel.Items
            .Where(item => item.ItemHierarchyId == 2 &&
                          item.ProductTypeId is 1 or 2 &&
                          item.Years == 0)
            .ToList();

        if (secondaryNewRenewals.Count == 0)
        {
            _logger.LogInformation("No secondary new/renewal items with 0 years; skipping");
            return preparedModel;
        }

        _logger.LogDebug("Found {ItemCount} secondary items with years==0", secondaryNewRenewals.Count);

        foreach (var item in secondaryNewRenewals)
        {
            _logger.LogDebug("Section 2.3.2: Item {LineItem} years 0 → 1", item.LineItem);
            item.Years = 1;

            _logger.LogInformation("Section 2.3.2: Item {LineItem} updated: Years=1", item.LineItem);
        }

        return preparedModel;
    }

    /// <summary>
    /// SECTION 2.3.3: Calculate storage for secondary upgrade items
    /// 
    /// For secondary items with Years == 0 (upgrades) and capacity pricing model (UsagePricingModelId == 2),
    /// updates StorageGb to the delta: item.storage_gb - license.storage_gb
    /// 
    /// Translates SQL UPDATE from Section 2.3.3 exactly.
    /// </summary>
    public async Task<CartOrderPreparedModel> DetermineSecondaryUpgradeStorageAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.3.3: Calculating storage for secondary upgrades");

        var siteId = preparedModel.SiteId ?? string.Empty;
        if (siteId == "SFDC")
        {
            _logger.LogDebug("Section 2.3.3: SFDC order; skipping storage calculation");
            return preparedModel;
        }

        var license = preparedModel.License;
        if (license?.UsagePricingModelId != 2)
        {
            _logger.LogDebug("Section 2.3.3: License usage_pricing_model_id {Model} != 2 (capacity); skipping",
                license?.UsagePricingModelId ?? 0);
            return preparedModel;
        }

        var secondaryUpgrades = preparedModel.Items
            .Where(item => item.Years == 0 &&
                          item.StorageGb.HasValue &&
                          license?.LicenseCategoryName == item.LicenseCategoryName)
            .ToList();

        if (secondaryUpgrades.Count == 0)
        {
            _logger.LogInformation("No secondary capacity upgrades found; skipping storage update");
            return preparedModel;
        }

        _logger.LogDebug("Found {UpgradeCount} secondary capacity upgrades", secondaryUpgrades.Count);

        foreach (var item in secondaryUpgrades)
        {
            var existingStorage = license?.StorageGb ?? 0;
            var newStorage = (item.StorageGb ?? 0) - existingStorage;

            _logger.LogDebug("Section 2.3.3: Item {LineItem} storage {OldStorage}GB → delta {Delta}GB",
                item.LineItem, item.StorageGb, newStorage);

            item.StorageGb = newStorage;

            _logger.LogInformation("Section 2.3.3: Item {LineItem} storage updated: {NewStorage}GB",
                item.LineItem, newStorage);
        }

        return preparedModel;
    }

    /// <summary>
    /// SECTION 2.4.1: Calculate years from SFDC date ranges
    /// 
    /// For SFDC orders, computes Years from the date range (ExpirationDate - StartDate):
    /// - <= 366 days → 1 year
    /// - 367-731 days → 2 years
    /// - > 731 days → 3 years
    /// 
    /// Updates only items where ProductTypeId != 3 (not upgrades).
    /// Translates SQL UPDATE from Section 2.4.1 exactly.
    /// </summary>
    public async Task<CartOrderPreparedModel> DetermineSfdcYearsFromDatesAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.4.1: Calculating years from SFDC date ranges");

        var siteId = preparedModel.SiteId ?? string.Empty;
        if (siteId != "SFDC")
        {
            _logger.LogDebug("Section 2.4.1: Not SFDC order; skipping");
            return preparedModel;
        }

        var sfdcItems = preparedModel.Items
            .Where(item => item.ProductTypeId != 3 &&
                          item.StartDate.HasValue &&
                          item.ExpirationDate.HasValue)
            .ToList();

        if (sfdcItems.Count == 0)
        {
            _logger.LogInformation("No SFDC items with date range; skipping");
            return preparedModel;
        }

        _logger.LogDebug("Found {ItemCount} SFDC items to compute years from dates", sfdcItems.Count);

        foreach (var item in sfdcItems)
        {
            if (!item.ExpirationDate.HasValue || !item.StartDate.HasValue)
            {
                continue;
            }

            var daysDiff = (item.ExpirationDate.Value.Date - item.StartDate.Value.Date).TotalDays;
            var computedYears = ComputeSfdcYearsFromDays(daysDiff);

            _logger.LogDebug("Section 2.4.1: Item {LineItem} days={Days} → years={Years}",
                item.LineItem, (int)daysDiff, computedYears);

            item.Years = computedYears;

            _logger.LogInformation("Section 2.4.1: Item {LineItem} years from dates: {Years}",
                item.LineItem, computedYears);
        }

        return preparedModel;
    }

    /// <summary>
    /// Computes years based on day range for SFDC orders.
    /// 
    /// Logic:
    /// - <= 366 days → 1
    /// - 367-731 days → 2
    /// - > 731 days → 3
    /// </summary>
    private decimal ComputeSfdcYearsFromDays(double daysDiff)
    {
        if (daysDiff <= 366)
        {
            _logger.LogDebug("SFDC years: {Days} days <= 366 → 1 year", (int)daysDiff);
            return 1;
        }

        if (daysDiff > 366 && daysDiff <= 731)
        {
            _logger.LogDebug("SFDC years: {Days} days in [367-731] → 2 years", (int)daysDiff);
            return 2;
        }

        _logger.LogDebug("SFDC years: {Days} days > 731 → 3 years", (int)daysDiff);
        return 3;
    }

    /// <summary>
    /// SECTION 2.5: Mark storage-based product upgrades
    /// 
    /// For items with StorageGb populated and ProductTypeId IS NULL,
    /// sets ProductTypeId = 3 (Upgrade).
    /// 
    /// Translates SQL UPDATE from Section 2.5 exactly.
    /// </summary>
    public async Task<CartOrderPreparedModel> DetermineStorageBasedProductTypeAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.5: Marking storage-based upgrades");

        var storageItems = preparedModel.Items
            .Where(item => item.StorageGb.HasValue &&
                          (!item.ProductTypeId.HasValue || item.ProductTypeId.Value == 0))
            .ToList();

        if (storageItems.Count == 0)
        {
            _logger.LogInformation("No items with storage without product_type; skipping");
            return preparedModel;
        }

        _logger.LogDebug("Found {ItemCount} items with storage to mark as upgrade", storageItems.Count);

        foreach (var item in storageItems)
        {
            _logger.LogDebug("Section 2.5: Item {LineItem} storage={Storage}GB marked as upgrade",
                item.LineItem, item.StorageGb);

            item.ProductTypeId = 3;

            _logger.LogInformation("Section 2.5: Item {LineItem} updated: ProductTypeId=3 (storage upgrade)",
                item.LineItem);
        }

        return preparedModel;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 2.1.2: WIFI Expiration Override
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 2.1.2: Apply WIFI vendor expiration date override.
    ///
    /// SQL equivalent:
    /// <code>
    /// -- Override the expiration date if WIFI since it's set by Apple or Google
    /// UPDATE items
    /// SET expiration_date = ISNULL(vendor_expiration_date, expiration_date)
    /// WHERE license_category_name = 'WIFI'
    ///   AND vendor_expiration_date IS NOT NULL
    /// </code>
    ///
    /// Purpose: For WIFI products, Apple and Google supply the expiration date via the
    /// vendor_expiration_date field (transmitted by the vendor's backend system).
    /// This override takes precedence over the calculated expiration_date.
    ///
    /// Non-fatal: Items without vendor_expiration_date are left unchanged.
    /// </summary>
    /// <param name="preparedModel">Prepared model containing items to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated prepared model.</returns>
    public async Task<CartOrderPreparedModel> ApplyWifiExpirationOverrideAsync(
        CartOrderPreparedModel preparedModel,
        CancellationToken ct = default)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        _logger.LogDebug("Starting Section 2.1.2: Applying WIFI expiration override");

        // Filter to WIFI items that have a vendor-supplied expiration date
        var wifiItems = preparedModel.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.LicenseCategoryName) &&
                          string.Equals(item.LicenseCategoryName, "WIFI", StringComparison.OrdinalIgnoreCase) &&
                          item.VendorExpirationDate.HasValue &&
                          item.ItemHierarchyId == 1)  // Only primary WIFI items
            .ToList();

        if (wifiItems.Count == 0)
        {
            _logger.LogDebug("Section 2.1.2: No WIFI items with vendor_expiration_date; skipping override");
            return preparedModel;
        }

        _logger.LogDebug("Section 2.1.2: Found {WifiCount} WIFI items to apply expiration override", wifiItems.Count);

        foreach (var item in wifiItems)
        {
            if (!item.VendorExpirationDate.HasValue)
                continue;

            var originalExpiration = item.ExpirationDate;

            // SQL: SET expiration_date = ISNULL(vendor_expiration_date, expiration_date)
            item.ExpirationDate = item.VendorExpirationDate;

            _logger.LogDebug(
                "Section 2.1.2: Item {LineItem} (WIFI) expiration override: " +
                "{OriginalExpiration:O} → {VendorExpiration:O}",
                item.LineItem, originalExpiration, item.VendorExpirationDate);

            _logger.LogInformation(
                "Section 2.1.2: Item {LineItem} (WIFI) expiration_date updated: {NewDate:O}",
                item.LineItem, item.ExpirationDate);
        }

        return preparedModel;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 2.2.1: Product ID Resolution
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 2.2.1: Resolve product ID from item context using product profile logic.
    ///
    /// SQL equivalent:
    /// <code>
    /// SELECT TOP 1 @product_id = product_id
    /// FROM fn_product_select_profile(
    ///     @product_line_id,
    ///     @license_category_id,
    ///     @years,
    ///     @quantity,
    ///     @storage_gb,
    ///     @duration_days,
    ///     @product_type_id,
    ///     @license_keycode_type_id,
    ///     @usage_pricing_model_id,
    ///     @retention_model_id,
    ///     @product_platform_id)
    /// ORDER BY product_id
    /// </code>
    ///
    /// Purpose: Selects the appropriate product ID based on comprehensive order/item parameters.
    /// This is the C# equivalent of the SQL table-valued function fn_product_select_profile().
    ///
    /// Calls the repository to execute fn_product_select_profile TVF with all parameters.
    ///
    /// Non-fatal: Returns null if no matching product is found.
    /// </summary>
    /// <param name="item">The item context containing product selection parameters.</param>
    /// <param name="productLineId">Product line discriminator (partner vs. consumer).</param>
    /// <param name="licenseCategoryId">License category (Standard, WIFI, OTSF, etc.).</param>
    /// <param name="durationDays">Total days in the subscription (calculated from dates).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved product ID, or null if not found.</returns>
    public async Task<int?> ResolveProductIdAsync(
        CartOrderItemContext item,
        int productLineId,
        int licenseCategoryId,
        int durationDays,
        CancellationToken ct = default)
    {
        if (item is null)
        {
            _logger.LogWarning("ResolveProductIdAsync: item context is null");
            return null;
        }

        _logger.LogDebug(
            "ResolveProductIdAsync: resolving product for LineItem={Line}, " +
            "ProductLine={ProductLine}, LicenseCategory={Category}, " +
            "Years={Years}, Quantity={Quantity}, Storage={Storage}GB, " +
            "Days={Days}, ProductType={ProductType}, UsagePricingModel={UsageModel}, " +
            "RetentionModel={RetentionModel}, ProductPlatform={Platform}",
            item.LineItem, productLineId, licenseCategoryId,
            item.Years ?? 0, item.Quantity, item.StorageGb ?? 0,
            durationDays, item.ProductTypeId, item.UsagePricingModelId,
            item.RetentionModelId, item.ProductPlatformId);

        try
        {
            // Call the repository to execute fn_product_select_profile TVF
            var productId = await _repository.ResolveProductIdAsync(
                productLineId,
                licenseCategoryId,
                item.Years.HasValue ? (int)item.Years.Value : (int?)null,
                item.Quantity,
                item.StorageGb,
                durationDays,
                item.ProductTypeId,
                item.LicenseKeycodeTypeId,
                item.UsagePricingModelId,
                item.RetentionModelId,
                item.ProductPlatformId,
                sapMaterialNumber: null,  // SAP material number not tracked in CartOrderItemContext
                ct);

            if (productId.HasValue && productId.Value > 0)
            {
                _logger.LogInformation(
                    "ResolveProductIdAsync: LineItem={Line} → product_id={ProductId}",
                    item.LineItem, productId);
                return productId;
            }

            _logger.LogWarning(
                "ResolveProductIdAsync: no product found for LineItem={Line}, ProductLine={Line2}, LicenseCategory={Category}",
                item.LineItem, productLineId, licenseCategoryId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ResolveProductIdAsync: error resolving product for LineItem={Line}",
                item.LineItem);
            return null;
        }
    }
}

/// <summary>
/// Product Selection Service interface.
/// 
/// Abstracts the SQL function fn_product_select_profile() to enable future migration
/// of product selection logic from SQL to C#.
/// 
/// Implementation: Deferred - currently calls back to SQL function via repository.
/// </summary>
public interface IProductSelectionService
{
    /// <summary>
    /// Selects the appropriate product ID based on order/item parameters.
    /// 
    /// Parameters correspond to fn_product_select_profile() inputs:
    /// - ProductLineId: Partner ID or consumer catalog
    /// - LicenseCategoryId: License type (e.g., Standard, WIFI, OTSF)
    /// - Years: Duration of purchase
    /// - Quantity: Number of licenses/seats
    /// - StorageGb: Storage capacity (for storage products)
    /// - DurationDays: Total days in the subscription period
    /// - ProductTypeId: 1=New, 2=Renewal, 3=Upgrade
    /// - LicenseKeycodeTypeId: Keycode type (for licensing)
    /// - UsagePricingModelId: Pricing model (e.g., 2=Capacity)
    /// - RetentionModelId: Retention/support model
    /// - ProductPlatformId: On-Prem (1), SaaS (2), etc.
    /// - SapMaterialNumber: SAP product code
    /// 
    /// Returns the ProductId from matching product profile.
    /// </summary>
    Task<int> SelectProductAsync(
        int productLineId,
        int licenseCategoryId,
        decimal years,
        int quantity,
        int? storageGb,
        int durationDays,
        int? productTypeId,
        int? licenseKeycodeTypeId,
        byte? usagePricingModelId,
        byte? retentionModelId,
        byte? productPlatformId,
        int? sapMaterialNumber,
        CancellationToken ct = default);
}
