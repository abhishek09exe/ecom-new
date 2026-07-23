using Microsoft.EntityFrameworkCore;
using ecom_new_api.Data;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Repositories;

/// <summary>
/// Entity Framework Core implementation of cart order data access.
/// 
/// SECTION 1: Simple Lookups (database access only - no business logic)
/// - Partner lookup by PartnerKey
/// - Currency resolution by CurrencyCode
/// - Vendor order code existence check
/// - Configuration lookups
/// 
/// Each method is a single async EF Core query with no business logic.
/// </summary>
public class CartOrderRepository : ICartOrderRepository
{
    private const int StubBusinessBillingModel = 110;
    private const int StubBusinessLicenseAttributeId = 11;

    private readonly CartOrderDbContext _db;
    private readonly ILogger<CartOrderRepository> _logger;

    public CartOrderRepository(CartOrderDbContext db, ILogger<CartOrderRepository> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1: Simple Lookups (Database Access Methods)
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.1: Partner Lookup by PartnerKey
    /// 
    /// Query: SELECT partner_id FROM partner WHERE partner_key = @partnerKey
    /// 
    /// Purpose: Resolve the partner GUID key (from cart extension JSON) to the internal
    /// partner_id integer used for foreign key relationships.
    /// 
    /// Returns: The partner_id if found, null if partner doesn't exist or partnerKey is empty.
    /// 
    /// EF Core: Single filtered query using AsNoTracking (read-only lookup)
    /// </summary>
    public async Task<int?> LookupPartnerIdByKeyAsync(
        string? partnerKey,
        CancellationToken ct = default)
    {
        // Early return: null/empty key returns null
        if (string.IsNullOrWhiteSpace(partnerKey))
        {
            _logger.LogDebug("PartnerKey is null or empty, skipping partner lookup");
            return null;
        }

        _logger.LogDebug("Looking up partner by key: {PartnerKey}", partnerKey);

        // Query: Find the partner by GUID key, return only the partner_id
        var partnerId = await _db.Partners
            .AsNoTracking()  // Read-only lookup, no change tracking needed
            .Where(p => p.PartnerKey.ToString() == partnerKey)
            .Select(p => (int?)p.PartnerId)
            .FirstOrDefaultAsync(ct);

        if (partnerId.HasValue)
        {
            _logger.LogDebug("Partner found: PartnerKey={PartnerKey}, PartnerId={PartnerId}", 
                partnerKey, partnerId.Value);
        }
        else
        {
            _logger.LogWarning("Partner not found for PartnerKey: {PartnerKey}", partnerKey);
        }

        return partnerId;
    }

    /// <summary>
    /// SECTION 1.2: Currency Resolution by CurrencyCode
    /// 
    /// Query: SELECT currency_id FROM currency WHERE currency_code = @currencyCode
    /// 
    /// Purpose: Resolve currency code string (USD, EUR, AUD, etc.) to the internal
    /// currency_id used for cart order foreign key.
    /// 
    /// Returns: The currency_id if found, null if currency doesn't exist or code is empty.
    /// 
    /// EF Core: Single filtered query using AsNoTracking (read-only lookup)
    /// </summary>
    public async Task<int?> LookupCurrencyIdByCodeAsync(
        string? currencyCode,
        CancellationToken ct = default)
    {
        // Early return: null/empty code returns null
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            _logger.LogDebug("CurrencyCode is null or empty, skipping currency lookup");
            return null;
        }

        _logger.LogDebug("Looking up currency by code: {CurrencyCode}", currencyCode);

        // Query: Find currency by 3-char code (case-insensitive), return only the currency_id
        var currencyId = await _db.Currencies
            .AsNoTracking()  // Read-only lookup, no change tracking needed
            .Where(c => c.CurrencyCode.ToUpper() == currencyCode.ToUpper())
            .Select(c => (int?)c.CurrencyId)
            .FirstOrDefaultAsync(ct);

        if (currencyId.HasValue)
        {
            _logger.LogDebug("Currency found: CurrencyCode={CurrencyCode}, CurrencyId={CurrencyId}", 
                currencyCode, currencyId.Value);
        }
        else
        {
            _logger.LogWarning("Currency not found for CurrencyCode: {CurrencyCode}", currencyCode);
        }

        return currencyId;
    }

    /// <summary>
    /// SECTION 1.3: Vendor Order Code Existence Check
    /// 
    /// Query: SELECT COUNT(*) FROM cart_order WHERE vendor_order_code = @vendorOrderCode
    /// 
    /// Purpose: Check if a vendor order code already exists. Used to determine if an
    /// INSERT or UPDATE should be performed (pivot point in cart creation workflow).
    /// 
    /// Returns: true if vendor order code exists, false if new
    /// 
    /// EF Core: Single count query using AsNoTracking (read-only lookup)
    /// </summary>
    public async Task<bool> VendorOrderCodeExistsAsync(
        string vendorOrderCode,
        CancellationToken ct = default)
    {
        // Early return: null/empty code means it doesn't exist
        if (string.IsNullOrWhiteSpace(vendorOrderCode))
        {
            _logger.LogDebug("VendorOrderCode is null or empty, returning false");
            return false;
        }

        _logger.LogDebug("Checking if vendor order code exists: {VendorOrderCode}", vendorOrderCode);

        // Query: Check existence of vendor_order_code (unique index = fast lookup)
        var exists = await _db.CartOrders
            .AsNoTracking()  // Read-only existence check, no change tracking needed
            .AnyAsync(c => c.VendorOrderCode == vendorOrderCode, cancellationToken: ct);

        _logger.LogDebug("Vendor order code exists={Exists} for code: {VendorOrderCode}", 
            exists, vendorOrderCode);

        return exists;
    }

    /// <summary>
    /// SECTION 1.4: Get Partner Full Entity by PartnerId
    /// 
    /// Query: SELECT * FROM partner WHERE partner_id = @partnerId
    /// 
    /// Purpose: Fetch complete partner entity for validation or further lookups
    /// (e.g., partner configuration defaults).
    /// 
    /// Returns: PartnerEntity if found, null if not found
    /// 
    /// EF Core: Single entity query using AsNoTracking (read-only lookup)
    /// </summary>
    public async Task<PartnerEntity?> GetPartnerByIdAsync(
        int partnerId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching partner entity: PartnerId={PartnerId}", partnerId);

        // Query: Get full partner entity by ID
        var partner = await _db.Partners
            .AsNoTracking()  // Read-only lookup, no change tracking needed
            .FirstOrDefaultAsync(p => p.PartnerId == partnerId, cancellationToken: ct);

        if (partner != null)
        {
            _logger.LogDebug("Partner fetched: PartnerId={PartnerId}, PartnerName={PartnerName}", 
                partner.PartnerId, partner.PartnerName);
        }
        else
        {
            _logger.LogWarning("Partner not found: PartnerId={PartnerId}", partnerId);
        }

        return partner;
    }

    /// <summary>
    /// SECTION 1.5: Get Currency Full Entity by CurrencyId
    /// 
    /// Query: SELECT * FROM currency WHERE currency_id = @currencyId
    /// 
    /// Purpose: Fetch complete currency entity for validation or display purposes.
    /// 
    /// Returns: CurrencyEntity if found, null if not found
    /// 
    /// EF Core: Single entity query using AsNoTracking (read-only lookup)
    /// </summary>
    public async Task<CurrencyEntity?> GetCurrencyByIdAsync(
        int currencyId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching currency entity: CurrencyId={CurrencyId}", currencyId);

        // Query: Get full currency entity by ID
        var currency = await _db.Currencies
            .AsNoTracking()  // Read-only lookup, no change tracking needed
            .FirstOrDefaultAsync(c => c.CurrencyId == currencyId, cancellationToken: ct);

        if (currency != null)
        {
            _logger.LogDebug("Currency fetched: CurrencyId={CurrencyId}, CurrencyCode={CurrencyCode}", 
                currency.CurrencyId, currency.CurrencyCode);
        }
        else
        {
            _logger.LogWarning("Currency not found: CurrencyId={CurrencyId}", currencyId);
        }

        return currency;
    }

    /// <summary>
    /// SECTION 1.6: Get Product Full Entity by ProductId
    /// 
    /// Query: SELECT * FROM product WHERE product_id = @productId
    /// 
    /// Purpose: Fetch complete product entity for validation during cart item insertion.
    /// 
    /// Returns: ProductEntity if found, null if product doesn't exist
    /// 
    /// EF Core: Single entity query using AsNoTracking (read-only lookup)
    /// </summary>
    public async Task<ProductEntity?> GetProductByIdAsync(
        int productId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching product entity: ProductId={ProductId}", productId);

        // Query: Get full product entity by ID
        var product = await _db.Products
            .AsNoTracking()  // Read-only lookup, no change tracking needed
            .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken: ct);

        if (product != null)
        {
            _logger.LogDebug("Product fetched: ProductId={ProductId}, Description={Description}", 
                product.ProductId, product.ProductDescription);
        }
        else
        {
            _logger.LogWarning("Product not found: ProductId={ProductId}", productId);
        }

        return product;
    }

    /// <summary>
    /// SECTION 1.6B: Batch Get Products with License Categories
    /// 
    /// Query: SELECT * FROM product WHERE product_id IN (@ids)
    ///        INCLUDE license_category
    /// 
    /// Purpose: Fetch multiple products with their license categories in a single query.
    /// Optimizes performance by avoiding N+1 query problem (1 query instead of 2N queries).
    /// 
    /// Returns: Dictionary mapping ProductId → ProductEntity (includes resolved LicenseCategory if present)
    /// 
    /// EF Core: Batch query with .Include() for related entity, using AsNoTracking (read-only lookup)
    /// </summary>
    public async Task<Dictionary<int, ProductEntity>> GetProductsByIdBatchAsync(
        IEnumerable<int> productIds,
        CancellationToken ct = default)
    {
        // Early return: null/empty list returns empty dictionary
        var ids = productIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0)
        {
            _logger.LogDebug("GetProductsByIdBatchAsync called with empty product ID list");
            return new Dictionary<int, ProductEntity>();
        }

        _logger.LogDebug("Batch fetching {ProductCount} products with license categories", ids.Count);

        try
        {
            // Query: Fetch all products in one query, include their license categories
            var products = await _db.Products
                .AsNoTracking()  // Read-only lookup, no change tracking needed
                .Where(p => ids.Contains(p.ProductId))
                .Include(p => p.LicenseCategory)  // ✅ Single Include to avoid N+1 queries
                .ToDictionaryAsync(p => p.ProductId, cancellationToken: ct);

            _logger.LogInformation(
                "Batch fetched {FetchedCount} of {RequestedCount} products with categories",
                products.Count, ids.Count);

            // Log any missing products
            if (products.Count < ids.Count)
            {
                var missing = ids.Except(products.Keys).ToList();
                _logger.LogWarning(
                    "Not found {MissingCount} products: {MissingIds}",
                    missing.Count, string.Join(", ", missing));
            }

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error batch fetching {ProductCount} products", ids.Count);
            throw;
        }
    }

    /// <summary>
    /// SECTION 1.7: Get License Category Full Entity by LicenseCategoryId
    /// 
    /// Query: SELECT * FROM license_category WHERE license_category_id = @licenseCategoryId
    /// 
    /// Purpose: Fetch complete license category entity for validation.
    /// 
    /// Returns: LicenseCategoryEntity if found, null if not found
    /// 
    /// EF Core: Single entity query using AsNoTracking (read-only lookup)
    /// </summary>
    public async Task<LicenseCategoryEntity?> GetLicenseCategoryByIdAsync(
        int licenseCategoryId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching license category entity: LicenseCategoryId={LicenseCategoryId}", 
            licenseCategoryId);

        // Query: Get full license category entity by ID
        var category = await _db.LicenseCategories
            .AsNoTracking()  // Read-only lookup, no change tracking needed
            .FirstOrDefaultAsync(lc => lc.LicenseCategoryId == licenseCategoryId, 
                cancellationToken: ct);

        if (category != null)
        {
            _logger.LogDebug("License category fetched: LicenseCategoryId={LicenseCategoryId}, Name={Name}", 
                category.LicenseCategoryId, category.LicenseCategoryName);
        }
        else
        {
            _logger.LogWarning("License category not found: LicenseCategoryId={LicenseCategoryId}", 
                licenseCategoryId);
        }

        return category;
    }

    /// <summary>
    /// SECTION 1.3 & 1.5: Get License by Keycode (License Profile)
    /// 
    /// Query: SELECT * FROM license WHERE keycode = @keycode
    ///        (with all profile data: category, seats, expiration, autorenew, retention, pricing, etc.)
    /// 
    /// Purpose: Load complete license profile (Section 1.5 fn_license_select_license_profile).
    /// Required by: ProductDeterminationService for all Section 2+ operations
    /// 
    /// Returns: LicenseEntity with all fields populated, null if keycode not found
    /// 
    /// EF Core: Include related LicenseCategory to fetch category name
    /// </summary>
    public async Task<LicenseEntity?> GetLicenseByKeycodeAsync(
        string keycode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keycode))
        {
            _logger.LogDebug("Keycode is null or empty, skipping license lookup");
            return null;
        }

        _logger.LogDebug("Fetching license entity by keycode: {Keycode}", keycode);

        try
        {
            // Query: Get full license entity with related category by keycode
            var license = await _db.Licenses
                .AsNoTracking()
                .Include(l => l.LicenseCategory)  // Fetch category for name reference
                .FirstOrDefaultAsync(l => l.Keycode == keycode, cancellationToken: ct);

            if (license != null)
            {
                _logger.LogDebug(
                    "License fetched: LicenseId={LicenseId}, Keycode={Keycode}, Category={Category}, " +
                    "CategoryType={CategoryType}, Autorenew={Autorenew}, RetentionModel={RetentionModel}",
                    license.LicenseId, license.Keycode, license.LicenseCategory?.LicenseCategoryName,
                    license.CategoryTypeName, license.AutorenewCycle, license.RetentionModelId);
            }
            else
            {
                _logger.LogWarning("License not found: Keycode={Keycode}", keycode);
            }

            return license;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching license by keycode: {Keycode}", keycode);
            throw;
        }
    }

    /// <summary>
    /// SECTION 1.3.3: Get License Message (Next Process Date)
    /// 
    /// Query: SELECT * FROM license_message WHERE license_id = @licenseId
    ///        AND message_type_id = (monthly renewal/process type)
    /// 
    /// Purpose: Load next_process_date for monthly-to-annual conversion (Sections 1.7, 1.7.1).
    /// 
    /// Returns: LicenseMessageEntity with next_process_date, null if not found
    /// 
    /// EF Core: Filtered query for the license_id
    /// </summary>
    public async Task<LicenseMessageEntity?> GetLicenseMessageByIdAsync(
        int licenseId,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching license message: LicenseId={LicenseId}", licenseId);

        try
        {
            // Query: Get license message for this license (typically one per license for monthly billing)
            var message = await _db.LicenseMessages
                .AsNoTracking()
                .Where(m => m.LicenseId == licenseId)
                .FirstOrDefaultAsync(cancellationToken: ct);

            if (message != null)
            {
                _logger.LogDebug(
                    "License message fetched: LicenseId={LicenseId}, NextProcessDate={NextProcessDate}",
                    message.LicenseId, message.NextProcessDate);
            }
            else
            {
                _logger.LogDebug("License message not found: LicenseId={LicenseId}", licenseId);
            }

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching license message: LicenseId={LicenseId}", licenseId);
            throw;
        }
    }

    /// <summary>
    /// SECTION 1.2.1: Get Locale Mapping
    /// 
    /// Query: SELECT * FROM locale_language_location WHERE locale = @locale
    ///        OR CALL fn_locale_to_lang_loc(@locale, @language_code OUTPUT, @location_code OUTPUT)
    /// 
    /// Purpose: Translate @locale (e.g., 'en_US') to language_code and location_code
    /// for Section 1.9 product line lookups and order context.
    /// 
    /// Returns: LocaleLanguageLocationEntity with language_code and location_code, 
    ///          null if locale not found (use defaults: "en" / "US")
    /// 
    /// EF Core: Simple filtered query for locale code
    /// </summary>
    public async Task<LocaleLanguageLocationEntity?> GetLocaleByCodeAsync(
        string locale,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            _logger.LogDebug("Locale is null or empty, skipping locale lookup");
            return null;
        }

        _logger.LogDebug("Fetching locale mapping: Locale={Locale}", locale);

        try
        {
            // Query: Get locale mapping
            var localeMapping = await _db.LocaleLanguageLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Locale != null && l.Locale.ToLower() == locale.ToLower(), 
                    cancellationToken: ct);

            if (localeMapping != null)
            {
                _logger.LogDebug(
                    "Locale mapping fetched: Locale={Locale}, Language={Language}, Location={Location}",
                    localeMapping.Locale, localeMapping.LanguageCode, localeMapping.LocationCode);
            }
            else
            {
                _logger.LogDebug("Locale mapping not found: Locale={Locale}", locale);
            }

            return localeMapping;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching locale mapping: Locale={Locale}", locale);
            throw;
        }
    }

    /// <summary>
    /// SECTION 1.9 & 1.9.1: Get Product Line by License Category and Locale
    /// 
    /// Query: SELECT * FROM license_category_product_line 
    ///        WHERE license_category_id = @licenseCategoryId
    ///        AND language_code = @languageCode
    ///        AND location_code = @locationCode
    /// 
    /// Purpose: Map license_category_id + locale to product_line_id for upgrade context.
    /// Used to determine which products are available for upgrades in a specific region.
    /// 
    /// Returns: LicenseCategoryProductLineEntity with product_line_id, 
    ///          null if mapping not found (product line lookup defaults to license.product_line_id)
    /// 
    /// EF Core: Multi-field filtered query
    /// </summary>
    public async Task<LicenseCategoryProductLineEntity?> GetProductLineByLicenseCategoryAndLocaleAsync(
        int licenseCategoryId,
        string? languageCode,
        string? locationCode,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Fetching product line mapping: LicenseCategoryId={LicenseCategoryId}, Language={Language}, Location={Location}",
            licenseCategoryId, languageCode, locationCode);

        try
        {
            // Default to "en" and "US" if not provided
            var lang = languageCode ?? "en";
            var loc = locationCode ?? "US";

            // Query: Get product line mapping for this category and locale
            var productLine = await _db.LicenseCategoryProductLines
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.LicenseCategoryId == licenseCategoryId &&
                    p.LanguageCode == lang &&
                    p.LocationCode == loc,
                    cancellationToken: ct);

            if (productLine != null)
            {
                _logger.LogDebug(
                    "Product line mapping fetched: LicenseCategoryId={LicenseCategoryId}, ProductLineId={ProductLineId}",
                    productLine.LicenseCategoryId, productLine.ProductLineId);
            }
            else
            {
                _logger.LogDebug(
                    "Product line mapping not found: LicenseCategoryId={LicenseCategoryId}, Language={Language}, Location={Location}",
                    licenseCategoryId, lang, loc);
            }

            return productLine;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching product line mapping: LicenseCategoryId={LicenseCategoryId}, Language={Language}, Location={Location}",
                licenseCategoryId, languageCode, locationCode);
            throw;
        }
    }

    /// <summary>
    /// SECTION 1.3: Detect Utility Billing Models
    ///
    /// Query: SELECT COUNT(*) FROM license_attribute_license_value
    ///        WHERE license_attribute_license_value = @value AND (is_utility = 1 OR EXISTS in config)
    ///
    /// Purpose: Determine if a billing model (license_attribute_license_value) belongs to the
    /// utility billing models set (Section 1.3 @UTILITY_BILLING_MODELS config table).
    ///
    /// Returns: True if the billing model is a utility model, false otherwise.
    ///
    /// EF Core: Single filtered count query using AsNoTracking (read-only lookup).
    /// </summary>
    public async Task<bool> IsUtilityBillingModelAsync(
        int billingModelId,
        CancellationToken ct = default)
    {
        if (billingModelId <= 0)
        {
            _logger.LogDebug("IsUtilityBillingModelAsync: invalid billing model ID {Id}", billingModelId);
            return false;
        }

        _logger.LogDebug("IsUtilityBillingModelAsync: checking billing model {Id}", billingModelId);

        try
        {
            // TODO: REPLACE WITH ACTUAL query to utility_billing_models config table
            // For now, check a hardcoded set of known utility billing models
            // Common utility billing models: 20, 21, 22, etc. (varies by implementation)
            var knownUtilityModels = new[] { 20, 21, 22, 23, 24, 25 };
            var isUtility = knownUtilityModels.Contains(billingModelId);

            _logger.LogDebug(
                "IsUtilityBillingModelAsync: billing model {Id} is utility={IsUtility}",
                billingModelId, isUtility);

            return await Task.FromResult(isUtility);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "IsUtilityBillingModelAsync: error checking billing model {Id}",
                billingModelId);
            return false;
        }
    }

    /// <summary>
    /// SECTION 1.3.2: Resolve License Attribute ID
    ///
    /// Query: SELECT license_attribute_id FROM dbo.license_attribute_license_value
    ///        WHERE license_attribute_license_value = @value
    ///
    /// Purpose: Resolve the license_attribute_id (foreign key) from the billing model value
    /// (license_attribute_license_value). Used for attribute fallback in Section 1.11.
    ///
    /// Returns: The license_attribute_id, or null if not found.
    ///
    /// EF Core: Single filtered query using AsNoTracking (read-only lookup).
    /// </summary>
    public async Task<int?> GetLicenseAttributeIdByValueAsync(
        int billingModelId,
        CancellationToken ct = default)
    {
        if (billingModelId <= 0)
        {
            _logger.LogDebug("GetLicenseAttributeIdByValueAsync: invalid billing model ID {Id}", billingModelId);
            return null;
        }

        _logger.LogDebug(
            "GetLicenseAttributeIdByValueAsync: resolving attribute ID for billing model {Id}",
            billingModelId);

        try
        {
            // TODO: REPLACE WITH ACTUAL query to license_attribute_license_value table
            // For now, use a simple mapping: attribute_id = billing_model / 10 (placeholder logic)
            // In production, query: SELECT license_attribute_id FROM license_attribute_license_value
            //                       WHERE license_attribute_license_value = @value
            var attributeId = billingModelId > 0 ? (billingModelId / 10) : (int?)null;

            if (attributeId.HasValue && attributeId.Value > 0)
                _logger.LogDebug(
                    "GetLicenseAttributeIdByValueAsync: billing model {BillingModel} → attribute ID {AttrId}",
                    billingModelId, attributeId);
            else
                _logger.LogWarning(
                    "GetLicenseAttributeIdByValueAsync: no attribute ID found for billing model {BillingModel}",
                    billingModelId);

            return await Task.FromResult(attributeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "GetLicenseAttributeIdByValueAsync: error resolving attribute ID for billing model {Id}",
                billingModelId);
            return null;
        }
    }

    /// <summary>
    /// SECTION 1.9.1.1 & 1.9.1.2: Get location-based billing model for business product lines.
    /// </summary>
    public async Task<(int? BillingModelId, int? LicenseAttributeId)?> GetLocationBasedBillingModelAsync(
        int productLineId,
        string? locationCode,
        IEnumerable<int>? licenseCategoryIds,
        CancellationToken ct = default)
    {
        if (productLineId <= 0 || string.IsNullOrWhiteSpace(locationCode) || licenseCategoryIds is null)
        {
            _logger.LogDebug(
                "GetLocationBasedBillingModelAsync: invalid params (productLine={Line}, location={Loc}, categories={Count})",
                productLineId, locationCode, licenseCategoryIds?.Count() ?? 0);
            return null;
        }

        _logger.LogDebug(
            "GetLocationBasedBillingModelAsync: lookup for ProductLine={ProductLine}, Location={Location}",
            productLineId, locationCode);

        try
        {
            var categoryList = licenseCategoryIds.ToList();
            if (categoryList.Count == 0)
                return null;

            // Temporary stub to preserve SQL control flow while data access is unavailable.
            // The required mapping table (license_category_product_line_license_attribute_license_value)
            // is not yet modeled in EF Core and this project currently has no database access.
            _logger.LogDebug(
                "GetLocationBasedBillingModelAsync: using temporary stub values BillingModelId={BillingModelId}, LicenseAttributeId={LicenseAttributeId}",
                StubBusinessBillingModel, StubBusinessLicenseAttributeId);
            return await Task.FromResult<(int? BillingModelId, int? LicenseAttributeId)?>(
                (StubBusinessBillingModel, StubBusinessLicenseAttributeId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "GetLocationBasedBillingModelAsync: error for ProductLine={ProductLine}, Location={Location}",
                productLineId, locationCode);
            return null;
        }
    }

    /// <summary>
    /// SECTION 1.9.1.1 & 1.9.1.2: Get DEFAULT_BUSINESS_BILLING_MODEL fallback.
    /// </summary>
    public async Task<(int? LicenseAttributeId, int? BillingModelId)?> GetBusinessDefaultBillingModelAsync(
        CancellationToken ct = default)
    {
        _logger.LogDebug("GetBusinessDefaultBillingModelAsync: fetching business default billing model");

        try
        {
            // TODO: Replace this stub with DEFAULT_BUSINESS_BILLING_MODEL query once
            // database access is available and required tables/config are modeled in EF Core.
            _logger.LogDebug(
                "GetBusinessDefaultBillingModelAsync: using temporary stub values LicenseAttributeId={LicenseAttributeId}, BillingModelId={BillingModelId}",
                StubBusinessLicenseAttributeId, StubBusinessBillingModel);

            return await Task.FromResult<(int? LicenseAttributeId, int? BillingModelId)?>(
                (StubBusinessLicenseAttributeId, StubBusinessBillingModel));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetBusinessDefaultBillingModelAsync: error fetching default");
            return null;
        }
    }

    /// <summary>
    /// SECTION 1.15: Calculate item storage GB using category/quantity/usage model rules.
    /// </summary>
    public async Task<int?> GetItemStorageGbAsync(
        int quantity,
        string licenseCategoryName,
        byte? usagePricingModelId,
        CancellationToken ct = default)
    {
        if (quantity <= 0 || string.IsNullOrWhiteSpace(licenseCategoryName))
        {
            _logger.LogDebug(
                "GetItemStorageGbAsync: invalid params (qty={Qty}, category={Cat})",
                quantity, licenseCategoryName);
            return null;
        }

        _logger.LogDebug(
            "GetItemStorageGbAsync: calculating for Category={Category}, Qty={Qty}, UsageModel={Model}",
            licenseCategoryName, quantity, usagePricingModelId);

        try
        {
            // TODO: REPLACE WITH ACTUAL EF Core implementation of fn_get_item_storage_gb
            // SELECT dbo.fn_get_item_storage_gb(@quantity, @category, @usage_model_id, ...)
            //
            // Logic should determine storage based on:
            // 1. Category-specific defaults
            // 2. Quantity multipliers
            // 3. Usage pricing model (e.g., capacity model = per-TB logic)
            //
            // For now, return placeholder defaults
            int? resolvedStorage = usagePricingModelId == 2
                ? 100  // Capacity model default
                : 10;  // Standard model default

            _logger.LogDebug(
                "GetItemStorageGbAsync: placeholder returns {Storage}GB for {Category}",
                resolvedStorage, licenseCategoryName);

            return await Task.FromResult(resolvedStorage);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "GetItemStorageGbAsync: error calculating storage for {Category}",
                licenseCategoryName);
            return null;
        }
    }

    /// <summary>
    /// SECTION 2.4.2 & 2.4.3: Resolve product ID using fn_product_select_profile.
    /// </summary>
    public async Task<int?> ResolveProductIdAsync(
        int productLineId,
        int licenseCategoryId,
        int? years,
        int quantity,
        int? storageGb,
        int durationDays,
        int? productTypeId,
        int? licenseKeycodeTypeId,
        byte? usagePricingModelId,
        byte? retentionModelId,
        byte? productPlatformId,
        string? sapMaterialNumber,
        CancellationToken ct = default)
    {
        if (productLineId <= 0 || licenseCategoryId <= 0)
        {
            _logger.LogDebug(
                "ResolveProductIdAsync: invalid product line {Line} or license category {Category}",
                productLineId, licenseCategoryId);
            return null;
        }

        _logger.LogDebug(
            "ResolveProductIdAsync: resolving product for ProductLine={Line}, LicenseCategory={Category}, " +
            "Years={Years}, Qty={Qty}, Storage={Storage}GB, Days={Days}, ProductType={Type}, " +
            "UsageModel={Usage}, RetentionModel={Retention}, Platform={Platform}",
            productLineId, licenseCategoryId, years ?? 0, quantity, storageGb ?? 0,
            durationDays, productTypeId, usagePricingModelId, retentionModelId, productPlatformId);

        try
        {
            // TODO: REPLACE WITH ACTUAL EF Core query to fn_product_select_profile TVF
            // SELECT TOP 1 product_id FROM fn_product_select_profile(
            //     @product_line_id, @license_category_id, @years, @quantity,
            //     @storage_gb, @duration_days, @product_type_id, @license_keycode_type_id,
            //     @usage_pricing_model_id, @retention_model_id, @product_platform_id, @sap_material_number)
            // ORDER BY product_id
            //
            // For now, placeholder that logs parameters
            _logger.LogDebug("ResolveProductIdAsync: fn_product_select_profile TVF call deferred to EF Core implementation");
            return null;  // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ResolveProductIdAsync: error resolving product for ProductLine={Line}, LicenseCategory={Category}",
                productLineId, licenseCategoryId);
            return null;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 2+: Write Methods (NOT IMPLEMENTED - Section 1 only)
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [NOT IMPLEMENTED] Inserts cart order and items. Placeholder for Section 2.
    /// </summary>
    public async Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request,
        CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "InsertCartOrderAsync is not yet implemented. " +
            "This is a Section 2+ operation requiring complex business logic.");
    }

    /// <summary>
    /// [NOT IMPLEMENTED] Selects full cart aggregate. Placeholder for Section 2.
    /// </summary>
    public async Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode,
        CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "SelectCartOrderAsync is not yet implemented. " +
            "This is a Section 2+ operation requiring complex business logic.");
    }

    /// <summary>
    /// [NOT IMPLEMENTED] Finds existing cart by key. Placeholder for Section 2.
    /// </summary>
    public async Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key,
        CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "FindExistingVendorOrderCodeByKeyAsync is not yet implemented. " +
            "This is a Section 2+ operation requiring complex business logic.");
    }

    /// <summary>
    /// [NOT IMPLEMENTED] Selects license options. Placeholder for Section 3.
    /// </summary>
    public async Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
        string keycode,
        CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "SelectLicenseOptionsAsync is not yet implemented. " +
            "This is a Section 3+ operation requiring complex business logic.");
    }

    /// <summary>
    /// [NOT IMPLEMENTED] Selects configure options. Placeholder for Section 3.
    /// </summary>
    public async Task<ConfigureResponse?> SelectConfigureAsync(
        string keycode,
        CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "SelectConfigureAsync is not yet implemented. " +
            "This is a Section 3+ operation requiring complex business logic.");
    }

    /// <summary>
    /// [NOT IMPLEMENTED] Selects upgrade options. Placeholder for Section 3.
    /// </summary>
    public async Task<UpgradeResponse?> SelectUpgradeAsync(
        string keycode,
        CancellationToken ct = default)
    {
        throw new NotImplementedException(
            "SelectUpgradeAsync is not yet implemented. " +
            "This is a Section 3+ operation requiring complex business logic.");
    }
}
