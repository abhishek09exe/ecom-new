using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Repositories;

/// <summary>
/// Data-access contract for cart orders.
/// Each method maps directly to one or more stored procedures or lookup queries.
///
/// SECTION 1: Simple Lookups (database access only - no business logic)
/// SECTION 2+: Write operations and complex queries (not yet implemented)
/// 
/// Note on identifiers: both usp_cart_select_cart_order and usp_cart_select_cart_order_item
/// take @vendor_order_code (not cart_order_id) as their lookup key. All read/select
/// methods here use vendorOrderCode accordingly.
/// </summary>
public interface ICartOrderRepository
{
    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1: Simple Lookups (Database Access Methods)
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SQL Section 1.1: Existing cart lookup by vendor order code.
    ///
    /// Maps to:
    ///   SELECT cart_order_id, currency_id
    ///   FROM dbo.cart_order
    ///   WHERE vendor_order_code = @vendor_order_code
    ///
    /// Read-only lookup: returns only cart_order_id and currency_id,
    /// or null when no matching vendor_order_code exists.
    /// </summary>
    Task<CartOrderLookupResponse?> GetCartLookupByVendorOrderCodeAsync(
        string vendorOrderCode,
        CancellationToken ct = default);

    /// <summary>
    /// SQL Section 1.2: Cart context lookup by cart_order_id.
    ///
    /// Maps to:
    ///   SELECT co.locale, co.site_id, cp.partner_id
    ///   FROM dbo.cart_order co
    ///   LEFT JOIN dbo.cart_order_partner cp ON cp.cart_order_id = co.cart_order_id
    ///   WHERE co.cart_order_id = @cart_order_id
    ///
    /// Read-only lookup: returns locale, site_id, and partner_id only,
    /// or null when no matching cart_order_id exists.
    /// </summary>
    Task<CartOrderContextLookupResponse?> GetCartContextByCartOrderIdAsync(
        int cartOrderId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.1: Partner Lookup by PartnerKey
    /// Resolves partner GUID to internal partner_id integer.
    /// </summary>
    Task<int?> LookupPartnerIdByKeyAsync(
        string? partnerKey,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.2: Currency Resolution by CurrencyCode
    /// Resolves currency code string (USD, EUR, AUD) to internal currency_id.
    /// </summary>
    Task<int?> LookupCurrencyIdByCodeAsync(
        string? currencyCode,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.3: Vendor Order Code Existence Check
    /// Checks if vendor_order_code already exists (determines INSERT vs UPDATE).
    /// </summary>
    Task<bool> VendorOrderCodeExistsAsync(
        string vendorOrderCode,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.4: Get Partner Full Entity
    /// Fetches complete PartnerEntity for validation or further lookups.
    /// </summary>
    Task<PartnerEntity?> GetPartnerByIdAsync(
        int partnerId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.5: Get Currency Full Entity
    /// Fetches complete CurrencyEntity for validation or display.
    /// </summary>
    Task<CurrencyEntity?> GetCurrencyByIdAsync(
        int currencyId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.6: Get Product Full Entity
    /// Fetches complete ProductEntity for validation during cart item insertion.
    /// </summary>
    Task<ProductEntity?> GetProductByIdAsync(
        int productId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.6B: Batch Get Products with License Categories
    /// Fetches multiple ProductEntities with their related LicenseCategories in a single query.
    /// 
    /// Performance Optimization: Uses .Include() to avoid N+1 query problem.
    /// Returns dictionary mapping ProductId → ProductEntity (includes LicenseCategory if present).
    /// </summary>
    Task<Dictionary<int, ProductEntity>> GetProductsByIdBatchAsync(
        IEnumerable<int> productIds,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.7: Get License Category Full Entity
    /// Fetches complete LicenseCategoryEntity for validation.
    /// </summary>
    Task<LicenseCategoryEntity?> GetLicenseCategoryByIdAsync(
        int licenseCategoryId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.4.2: Load license categories for name-to-id assignment.
    ///
    /// Maps to:
    ///   SELECT license_category_id, license_category_name
    ///   FROM dbo.license_category
    ///
    /// Returns a lookup dictionary keyed by license_category_name.
    /// </summary>
    Task<Dictionary<string, int>> GetLicenseCategoryIdLookupByNameAsync(
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.3 & 1.5: Get License by Keycode
    /// Fetches complete LicenseEntity with all profile data (Section 1.5 fn_license_select_license_profile).
    /// 
    /// Returns full license with:
    /// - License ID, Category ID, Seats, Expiration, AutorenewCycle
    /// - RetentionModelId, RetentionTerm, UsagePricingModelId, ProductPlatformId
    /// - LicenseKeycodeTypeId, LicenseDistributionMethodId, StorageGb, CategoryTypeName, ProductLineId
    /// </summary>
    Task<LicenseEntity?> GetLicenseByKeycodeAsync(
        string keycode,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.3.3: Get License Message (Next Process Date)
    /// Fetches next_process_date for monthly billing conversions (Sections 1.7, 1.7.1).
    /// </summary>
    Task<LicenseMessageEntity?> GetLicenseMessageByIdAsync(
        int licenseId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.2.1: Get Locale Mapping
    /// Maps @locale (e.g., 'en_US') to language_code and location_code
    /// Used for Section 1.9 product line lookups.
    /// </summary>
    Task<LocaleLanguageLocationEntity?> GetLocaleByCodeAsync(
        string locale,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.9 & 1.9.1: Get Product Line by License Category
    /// Maps license_category_id + locale to product_line_id for upgrade context.
    /// </summary>
    Task<LicenseCategoryProductLineEntity?> GetProductLineByLicenseCategoryAndLocaleAsync(
        int licenseCategoryId,
        string? languageCode,
        string? locationCode,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.3: Detect Utility Billing Models
    /// Checks if a billing model (license_attribute_license_value) is in the utility set.
    ///
    /// Maps to: SELECT COUNT(*) FROM license_attribute_license_value WHERE license_attribute_license_value = @value
    /// AND is_utility = 1 (or similar config check).
    ///
    /// Returns true if billing model is a utility model, false otherwise.
    /// </summary>
    Task<bool> IsUtilityBillingModelAsync(
        int billingModelId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.3.2: Resolve License Attribute ID
    /// Resolves license_attribute_id from license_attribute_license_value.
    ///
    /// Maps to:
    ///   SELECT license_attribute_id FROM dbo.license_attribute_license_value
    ///   WHERE license_attribute_license_value = @value
    ///
    /// Returns the license_attribute_id, or null if not found.
    /// </summary>
    Task<int?> GetLicenseAttributeIdByValueAsync(
        int billingModelId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.7: Get existing license billing model from license_attribute_license.
    ///
    /// Maps to:
    ///   SELECT TOP (1) license_attribute_license_value
    ///   FROM dbo.license_attribute_license
    ///   WHERE license_id = @license_id
    ///
    /// Returns the existing license billing model, or null if no row exists.
    /// </summary>
    Task<int?> GetLicenseAttributeLicenseValueAsync(
        int licenseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// SECTION 1.9.1.1 & 1.9.1.2: Get location-based billing model for business product lines.
    /// 
    /// Maps to:
    ///   SELECT lav.license_attribute_license_value, lv.license_attribute_id
    ///   FROM license_category_product_line pl
    ///   INNER JOIN license_category_product_line_license_attribute_license_value lav
    ///     ON pl.license_category_product_line_id = lav.license_category_product_line_id
    ///   INNER JOIN license_attribute_license_value lv
    ///     ON lv.license_attribute_license_value = lav.license_attribute_license_value
    ///   WHERE pl.product_line_id = @productLineId
    ///     AND pl.location_code = @locationCode
    ///     AND pl.license_category_id IN (SELECT license_category_id FROM @item_table)
    /// 
    /// Returns: (billingModelId, licenseAttributeId) if found for the location, else null.
    /// </summary>
    Task<(int? BillingModelId, int? LicenseAttributeId)?> GetLocationBasedBillingModelAsync(
        int productLineId,
        string? locationCode,
        IEnumerable<int>? licenseCategoryIds,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.9.1.2: Check whether product_line_id is in BUSINESS_PRODUCT_LINE config.
    ///
    /// Maps to:
    ///   EXISTS (SELECT 1 FROM fn_app_config_select_key_values('BUSINESS_PRODUCT_LINE','GENERAL') WHERE [key] = @product_line_id)
    /// </summary>
    Task<bool> IsBusinessProductLineAsync(
        int productLineId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.9.1.1 & 1.9.1.2: Get DEFAULT_BUSINESS_BILLING_MODEL for fallback.
    /// 
    /// Maps to:
    ///   SELECT license_attribute_id, license_attribute_license_value
    ///   FROM license_attribute_license_value lv
    ///   WHERE lv.license_attribute_license_value IN (SELECT license_attribute_license_value FROM @DEFAULT_BUSINESS_BILLING_MODEL)
    ///   (Config-driven set of business default billing models)
    /// 
    /// Returns: (licenseAttributeId, billingModelId) or null if not configured.
    /// </summary>
    Task<(int? LicenseAttributeId, int? BillingModelId)?> GetBusinessDefaultBillingModelAsync(
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.9.1.2: Check whether billing model is in DEFAULT_BUSINESS_BILLING_MODEL config.
    ///
    /// Maps to:
    ///   EXISTS (SELECT 1 FROM fn_app_config_select_key_values('DEFAULT_BUSINESS_BILLING_MODEL','GENERAL') WHERE [key] = @license_attribute_license_value)
    /// </summary>
    Task<bool> IsDefaultBusinessBillingModelAsync(
        int billingModelId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.12 Step 2: Resolve partner-specific usage pricing model by category.
    ///
    /// Maps to:
    ///   SELECT m.usage_pricing_model_id
    ///   FROM dbo.license_category lc
    ///   INNER JOIN dbo.partner_usage_pricing_model m
    ///     ON lc.license_category_id = m.license_category_id
    ///   WHERE m.partner_id = @partnerId
    ///     AND m.site_id = @siteId
    ///     AND lc.license_category_name = @licenseCategoryName
    ///
    /// Returns (Found, UsagePricingModelId). Found=true with null UsagePricingModelId means
    /// a matching row exists and SQL ISNULL fallback to 1 should be applied by caller.
    /// </summary>
    Task<(bool Found, byte? UsagePricingModelId)> GetPartnerUsagePricingModelByCategoryAsync(
        int partnerId,
        string? siteId,
        string? licenseCategoryName,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.14 Step 2: Resolve partner-specific product platform by category.
    ///
    /// Maps to:
    ///   SELECT m.product_platform_id
    ///   FROM dbo.license_category lc
    ///   INNER JOIN dbo.partner_product_platform m
    ///     ON lc.license_category_id = m.license_category_id
    ///   WHERE m.partner_id = @partnerId
    ///     AND m.site_id = @siteId
    ///     AND lc.license_category_name = @licenseCategoryName
    ///
    /// Returns (Found, ProductPlatformId). Found=true with null ProductPlatformId means
    /// a matching row exists and SQL ISNULL fallback to 1 should be applied by caller.
    /// </summary>
    Task<(bool Found, byte? ProductPlatformId)> GetPartnerProductPlatformByCategoryAsync(
        int partnerId,
        string? siteId,
        string? licenseCategoryName,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.13 Step 2: Resolve partner-specific retention model by category.
    ///
    /// Maps to:
    ///   SELECT m.retention_model_id
    ///   FROM dbo.license_category lc
    ///   INNER JOIN dbo.partner_retention_model m
    ///     ON lc.license_category_id = m.license_category_id
    ///   WHERE m.partner_id = @partnerId
    ///     AND m.site_id = @siteId
    ///     AND lc.license_category_name = @licenseCategoryName
    ///
    /// Returns (Found, RetentionModelId). Found=true with null RetentionModelId means
    /// a matching row exists and SQL ISNULL fallback to 1 should be applied by caller.
    /// </summary>
    Task<(bool Found, byte? RetentionModelId)> GetPartnerRetentionModelByCategoryAsync(
        int partnerId,
        string? siteId,
        string? licenseCategoryName,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 1.15: Calculate item storage GB using category/quantity/usage model rules.
    /// 
    /// Equivalent to fn_get_item_storage_gb(quantity, license_category_name, usage_pricing_model_id).
    /// 
    /// Returns resolved storage in GB, or null if unable to calculate.
    /// </summary>
    Task<int?> GetItemStorageGbAsync(
        int quantity,
        string licenseCategoryName,
        byte? usagePricingModelId,
        CancellationToken ct = default);

    /// <summary>
    /// SECTION 2.4.2 & 2.4.3: Resolve product ID using fn_product_select_profile logic.
    /// 
    /// Maps to:
    ///   SELECT TOP 1 product_id FROM fn_product_select_profile(
    ///       @product_line_id, @license_category_id, @years, @quantity,
    ///       @storage_gb, @duration_days, @product_type_id, @license_keycode_type_id,
    ///       @usage_pricing_model_id, @retention_model_id, @product_platform_id, @sap_material_number)
    ///   ORDER BY product_id
    /// 
    /// Returns: The resolved product ID for the given configuration, or null if not found.
    /// </summary>
    Task<int?> ResolveProductIdAsync(
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
        CancellationToken ct = default);

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 2+: Write Methods (NOT IMPLEMENTED)
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Inserts the cart header and all related rows, then returns the generated vendor_order_code.
    ///
    /// Maps to:
    ///   usp_cart_insert_cart_order(@site_id, @locale, @user_ip, @cart_extension_json,
    ///       @response_code OUTPUT, @message OUTPUT)
    ///   usp_cart_insert_cart_order_item(@vendor_order_code, @item_json, @bundle_json,
    ///       @response_code OUTPUT, @message OUTPUT)  — called once per item in request.Items
    ///
    /// Returns the generated vendor_order_code (used as the key for all subsequent reads).
    /// </summary>
    Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Re-reads the full cart aggregate (header + items) after insert/update.
    ///
    /// Maps to:
    ///   usp_cart_select_cart_order(@vendor_order_code) — header row
    ///   usp_cart_select_cart_order_item(@vendor_order_code) — item rows
    ///
    /// This is what the API returns — NOT the raw insert output.
    /// The frontend depends on computed fields (pricing, equivalent_year_price, vault, etc.)
    /// that are only present in this re-read.
    /// </summary>
    Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default);

    /// <summary>
    /// Re-reads the full cart aggregate (header + items) by <c>cart_order_id</c>.
    ///
    /// Purpose: Allows callers that already resolved SQL Section 1.1
    /// (<c>cart_order_id</c> by <c>vendor_order_code</c>) to avoid querying
    /// <c>cart_order</c> a second time by vendor code.
    ///
    /// Returns the same hydrated shape as <see cref="SelectCartOrderAsync(string, CancellationToken)"/>.
    /// </summary>
    Task<CartOrderResponse?> SelectCartOrderByIdAsync(
        int cartOrderId, CancellationToken ct = default);

    // ── Quote-key check (pivot create → update) ─────────────────────────────────

    /// <summary>
    /// Checks whether the given key resolves to an existing pending (quote) cart.
    /// If it does, the service must call UpdateCartOrderAsync instead of inserting.
    ///
    /// Maps to: usp_cart_select_message_key (partial — checks cart_order_message
    /// joined to cart_order where message_key = @key and status = pending/quote).
    ///
    /// Returns the vendor_order_code of the existing cart if found, else null.
    ///
    /// TODO: REPLACE WITH ACTUAL — query message_key / cart_order tables.
    /// </summary>
    Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key, CancellationToken ct = default);

    // ── Read path (GET endpoints) ───────────────────────────────────────────────

    /// <summary>
    /// Fetches license + available products for a keycode.
    ///
    /// Maps to:
    ///   usp_cart_select_message_key(@key) — resolves keycode to license record
    ///   usp_license_select_license_by_id(@license_id) — license details
    ///   usp_cart_select_license_profile(@license_id) — trial/full product profile
    ///   usp_cart_select_license_billing_model(@license_id) — billing model tooltip data
    ///
    /// TODO: REPLACE WITH ACTUAL — implement when DB is available.
    /// </summary>
    Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
        string keycode, CancellationToken ct = default);

    /// <summary>
    /// Returns renewal product options for a license.
    ///
    /// Maps to: usp_partner_cart_select_order_page_details (renew context)
    ///
    /// TODO: REPLACE WITH ACTUAL — implement when DB is available.
    /// </summary>
    Task<ConfigureResponse?> SelectConfigureAsync(
        string keycode, CancellationToken ct = default);

    /// <summary>
    /// Returns upgrade product options for a license.
    ///
    /// Maps to: usp_product_select_license_category_upgrade
    ///
    /// TODO: REPLACE WITH ACTUAL — implement when DB is available.
    /// </summary>
    Task<UpgradeResponse?> SelectUpgradeAsync(
        string keycode, CancellationToken ct = default);
}

