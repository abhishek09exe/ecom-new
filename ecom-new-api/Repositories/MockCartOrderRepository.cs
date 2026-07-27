using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Repositories;

/// <summary>
/// Mock repository — returns hard-coded/in-memory data so the service and controller
/// layers can be developed and tested without a real database connection.
///
/// REPLACE THIS CLASS with a real implementation (EF Core + SqlClient) once DB access
/// is available. Register the real implementation in Program.cs in place of this one.
///
/// Each method documents the stored procedure(s) it must call and the exact behavior
/// it must reproduce from the PHP layer.
/// </summary>
public sealed class MockCartOrderRepository : ICartOrderRepository
{
    // ── In-memory store (development only) ─────────────────────────────────────
    private static int _nextId = 1000;
    private static readonly Dictionary<string, CartOrderResponse> _store = new();

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1: Simple Lookups (Mock implementations for development)
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mock: Partner lookup by key — returns null (no partner in mock)
    /// </summary>
    public Task<int?> LookupPartnerIdByKeyAsync(
        string? partnerKey,
        CancellationToken ct = default)
    {
        // Mock: always returns null (no partners in mock environment)
        return Task.FromResult<int?>(null);
    }

    /// <summary>
    /// Mock: Currency lookup by code — returns default currency (1 = USD)
    /// </summary>
    public Task<int?> LookupCurrencyIdByCodeAsync(
        string? currencyCode,
        CancellationToken ct = default)
    {
        // Mock: USD = 1 (default), EUR = 2, AUD = 3, etc.
        // In production, this queries the currency table
        return Task.FromResult<int?>(currencyCode?.ToUpper() switch
        {
            "USD" => 1,
            "EUR" => 2,
            "AUD" => 3,
            "GBP" => 4,
            "CAD" => 5,
            _ => null  // unknown currency
        });
    }

    /// <summary>
    /// Mock: Vendor order code existence check — checks in-memory store
    /// </summary>
    public Task<bool> VendorOrderCodeExistsAsync(
        string vendorOrderCode,
        CancellationToken ct = default)
    {
        // Mock: check if code exists in our in-memory store
        return Task.FromResult(_store.ContainsKey(vendorOrderCode));
    }

    /// <summary>
    /// SQL Section 1.1 mock lookup by vendor order code.
    /// Returns only cart_order_id and currency_id from the in-memory store.
    /// </summary>
    public Task<CartOrderLookupResponse?> GetCartLookupByVendorOrderCodeAsync(
        string vendorOrderCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vendorOrderCode))
        {
            return Task.FromResult<CartOrderLookupResponse?>(null);
        }

        if (!_store.TryGetValue(vendorOrderCode, out var order))
        {
            return Task.FromResult<CartOrderLookupResponse?>(null);
        }

        return Task.FromResult<CartOrderLookupResponse?>(new CartOrderLookupResponse
        {
            CartOrderId = order.CartOrderId,
            CurrencyId = order.CurrencyId
        });
    }

    /// <summary>
    /// SQL Section 1.2 mock lookup by cart_order_id.
    /// Returns only locale, site_id, and partner_id from the in-memory store.
    /// </summary>
    public Task<CartOrderContextLookupResponse?> GetCartContextByCartOrderIdAsync(
        int cartOrderId,
        CancellationToken ct = default)
    {
        if (cartOrderId <= 0)
        {
            return Task.FromResult<CartOrderContextLookupResponse?>(null);
        }

        var order = _store.Values.FirstOrDefault(x => x.CartOrderId == cartOrderId);
        if (order is null)
        {
            return Task.FromResult<CartOrderContextLookupResponse?>(null);
        }

        return Task.FromResult<CartOrderContextLookupResponse?>(new CartOrderContextLookupResponse
        {
            Locale = order.Locale,
            SiteId = order.SiteId,
            PartnerId = null
        });
    }

    /// <summary>
    /// Mock: Get partner entity — returns null (no partners in mock)
    /// </summary>
    public Task<PartnerEntity?> GetPartnerByIdAsync(
        int partnerId,
        CancellationToken ct = default)
    {
        // Mock: no partners in mock environment
        return Task.FromResult<PartnerEntity?>(null);
    }

    /// <summary>
    /// Mock: Get currency entity — returns mock currency
    /// </summary>
    public Task<CurrencyEntity?> GetCurrencyByIdAsync(
        int currencyId,
        CancellationToken ct = default)
    {
        // Mock: return a simple currency entity
        var currency = new CurrencyEntity
        {
            CurrencyId = currencyId,
            CurrencyCode = currencyId switch
            {
                1 => "USD",
                2 => "EUR",
                3 => "AUD",
                4 => "GBP",
                5 => "CAD",
                _ => "UNKNOWN"
            },
            CurrencyName = currencyId switch
            {
                1 => "US Dollar",
                2 => "Euro",
                3 => "Australian Dollar",
                4 => "British Pound",
                5 => "Canadian Dollar",
                _ => "Unknown Currency"
            }
        };
        return Task.FromResult<CurrencyEntity?>(currency);
    }

    /// <summary>
    /// Mock: Get product entity — returns null (limited product data in mock)
    /// </summary>
    public Task<ProductEntity?> GetProductByIdAsync(
        int productId,
        CancellationToken ct = default)
    {
        // Mock: return a simple product entity for testing
        if (productId <= 0)
            return Task.FromResult<ProductEntity?>(null);

        var product = new ProductEntity
        {
            ProductId = productId,
            ProductDescription = $"Mock Product {productId}",
            ProductTypeId = 1,
            ProductFamilyId = 1,
            ProductLineId = 1,
            ProductLifecycleId = 1,
            LicenseKeycodeTypeId = 1,
            LicenseCategoryId = 1
        };
        return Task.FromResult<ProductEntity?>(product);
    }

    /// <summary>
    /// Mock: Batch get products with categories — optimized mock implementation
    /// </summary>
    public Task<Dictionary<int, ProductEntity>> GetProductsByIdBatchAsync(
        IEnumerable<int> productIds,
        CancellationToken ct = default)
    {
        var result = new Dictionary<int, ProductEntity>();

        if (productIds is null)
            return Task.FromResult(result);

        foreach (var productId in productIds.Distinct())
        {
            if (productId <= 0)
                continue;

            var product = new ProductEntity
            {
                ProductId = productId,
                ProductDescription = $"Mock Product {productId}",
                ProductTypeId = 1,
                ProductFamilyId = 1,
                ProductLineId = 1,
                ProductLifecycleId = 1,
                LicenseKeycodeTypeId = 1,
                LicenseCategoryId = 1,
                LicenseCategory = new LicenseCategoryEntity
                {
                    LicenseCategoryId = 1,
                    LicenseCategoryName = "SMB",
                    LicenseCategoryDescription = "Small/Medium Business"
                }
            };

            result[productId] = product;
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Mock: Get license category entity — returns default category
    /// </summary>
    public Task<LicenseCategoryEntity?> GetLicenseCategoryByIdAsync(
        int licenseCategoryId,
        CancellationToken ct = default)
    {
        // Mock: return a simple license category
        if (licenseCategoryId <= 0)
            return Task.FromResult<LicenseCategoryEntity?>(null);

        var category = new LicenseCategoryEntity
        {
            LicenseCategoryId = licenseCategoryId,
            LicenseCategoryName = licenseCategoryId switch
            {
                1 => "SOHO",
                2 => "SMB",
                3 => "ENT",
                4 => "OTSF",
                5 => "CBEP",
                _ => "UNKNOWN"
            },
            LicenseCategoryDescription = null
        };
        return Task.FromResult<LicenseCategoryEntity?>(category);
    }

    /// <summary>
    /// Mock: Get license category lookup by name.
    /// </summary>
    public Task<Dictionary<string, int>> GetLicenseCategoryIdLookupByNameAsync(
        CancellationToken ct = default)
    {
        var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["SOHO"] = 1,
            ["SMB"] = 2,
            ["ENT"] = 3,
            ["OTSF"] = 4,
            ["CBEP"] = 5
        };

        return Task.FromResult(lookup);
    }

    /// <summary>
    /// Mock: Get license by keycode — returns mock license with all profile data
    /// </summary>
    public Task<LicenseEntity?> GetLicenseByKeycodeAsync(
        string keycode,
        CancellationToken ct = default)
    {
        // Mock: return mock license data
        if (string.IsNullOrWhiteSpace(keycode))
            return Task.FromResult<LicenseEntity?>(null);

        var license = new LicenseEntity
        {
            LicenseId = 1001,
            Keycode = keycode,
            LicenseCategoryId = 2,  // SMB
            LicenseStatus = "ACTIVE",
            LicenseSeats = 5,
            ExpirationDate = DateTime.UtcNow.AddYears(1),
            AutorenewCycle = 1,
            RetentionModelId = 1,
            RetentionTerm = 1,
            UsagePricingModelId = null,
            ProductPlatformId = 1,
            LicenseKeycodeTypeId = 1,
            LicenseDistributionMethodId = 1,
            StorageGb = null,
            CategoryTypeName = "full",
            ProductLineId = 1,
            LicenseCategory = new LicenseCategoryEntity
            {
                LicenseCategoryId = 2,
                LicenseCategoryName = "SMB",
                LicenseCategoryDescription = "Small/Medium Business"
            }
        };
        return Task.FromResult<LicenseEntity?>(license);
    }

    /// <summary>
    /// Mock: Get license message — returns null (no next process date in mock)
    /// </summary>
    public Task<LicenseMessageEntity?> GetLicenseMessageByIdAsync(
        int licenseId,
        CancellationToken ct = default)
    {
        // Mock: return next process date (30 days from now for billing conversion)
        var message = new LicenseMessageEntity
        {
            LicenseMessageId = 1,
            LicenseId = licenseId,
            MessageTypeId = 1,
            NextProcessDate = DateTime.UtcNow.AddDays(30),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        };
        return Task.FromResult<LicenseMessageEntity?>(message);
    }

    /// <summary>
    /// Mock: Get locale mapping — returns standard locale mappings
    /// </summary>
    public Task<LocaleLanguageLocationEntity?> GetLocaleByCodeAsync(
        string locale,
        CancellationToken ct = default)
    {
        // Mock: map common locales to language/location codes
        if (string.IsNullOrWhiteSpace(locale))
            return Task.FromResult<LocaleLanguageLocationEntity?>(null);

        var mapping = locale.ToLower() switch
        {
            "en_us" => ("en", "US"),
            "en_gb" => ("en", "GB"),
            "de_de" => ("de", "DE"),
            "fr_fr" => ("fr", "FR"),
            "ja_jp" => ("ja", "JP"),
            _ => ("en", "US")  // default to en_US
        };

        var entity = new LocaleLanguageLocationEntity
        {
            LocaleLanguageLocationId = 1,
            Locale = locale,
            LanguageCode = mapping.Item1,
            LocationCode = mapping.Item2
        };
        return Task.FromResult<LocaleLanguageLocationEntity?>(entity);
    }

    /// <summary>
    /// Mock: Get product line by license category and locale
    /// </summary>
    public Task<LicenseCategoryProductLineEntity?> GetProductLineByLicenseCategoryAndLocaleAsync(
        int licenseCategoryId,
        string? languageCode,
        string? locationCode,
        CancellationToken ct = default)
    {
        // Mock: return default product line (1) for all categories
        var productLine = new LicenseCategoryProductLineEntity
        {
            LicenseCategoryProductLineId = 1,
            LicenseCategoryId = licenseCategoryId,
            ProductLineId = 1,
            LanguageCode = languageCode ?? "en",
            LocationCode = locationCode ?? "US"
        };
        return Task.FromResult<LicenseCategoryProductLineEntity?>(productLine);
    }

    /// <inheritdoc/>
    /// Mock: Checks if a billing model is in the utility set. Always returns false in mock.
    public Task<bool> IsUtilityBillingModelAsync(
        int billingModelId,
        CancellationToken ct = default)
    {
        // Mock: no utility models in mock environment
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    /// Mock: Resolves license_attribute_id from billing model value.
    /// Returns a deterministic value based on the input for testing.
    public Task<int?> GetLicenseAttributeIdByValueAsync(
        int billingModelId,
        CancellationToken ct = default)
    {
        // Mock: simple mapping for testing
        // Common billing models: 110=annual, 12=monthly, etc.
        // Assume attribute IDs are similar values for mock purposes
        return Task.FromResult<int?>(billingModelId > 0 ? billingModelId : null);
    }

    /// <inheritdoc/>
    /// Mock: Resolve existing license billing model from license_attribute_license by license ID.
    public Task<int?> GetLicenseAttributeLicenseValueAsync(
        int licenseId,
        CancellationToken cancellationToken = default)
    {
        // Mock: use a deterministic monthly model value for valid IDs
        // to exercise 1.7/1.7.1 conversion paths.
        return Task.FromResult<int?>(licenseId > 0 ? 12 : null);
    }

    /// <summary>Mock implementation of location-based billing model lookup.</summary>
    public Task<(int? BillingModelId, int? LicenseAttributeId)?> GetLocationBasedBillingModelAsync(
        int productLineId,
        string? locationCode,
        IEnumerable<int>? licenseCategoryIds,
        CancellationToken ct = default)
    {
        // Mock: return null (no location-specific override in mock data)
        return Task.FromResult<(int?, int?)?>(null);
    }

    /// <inheritdoc/>
    public Task<bool> IsBusinessProductLineAsync(
        int productLineId,
        CancellationToken ct = default)
    {
        // Mock: treat common lines as business.
        return Task.FromResult(productLineId is 300 or 55);
    }

    /// <summary>Mock implementation of business default billing model lookup.</summary>
    public Task<(int? LicenseAttributeId, int? BillingModelId)?> GetBusinessDefaultBillingModelAsync(
        CancellationToken ct = default)
    {
        // Mock: return hardcoded business default (110 = annual)
        return Task.FromResult<(int?, int?)?>(((int?)11, (int?)110));
    }

    /// <inheritdoc/>
    public Task<bool> IsDefaultBusinessBillingModelAsync(
        int billingModelId,
        CancellationToken ct = default)
    {
        // Mock: match the mock default billing model value.
        return Task.FromResult(billingModelId == 110);
    }

    /// <inheritdoc/>
    public Task<(bool Found, byte? UsagePricingModelId)> GetPartnerUsagePricingModelByCategoryAsync(
        int partnerId,
        string? siteId,
        string? licenseCategoryName,
        CancellationToken ct = default)
    {
        if (partnerId <= 0 || string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(licenseCategoryName))
            return Task.FromResult((false, (byte?)null));

        // Mock examples for deterministic behavior in tests.
        if (string.Equals(licenseCategoryName, "OTSF", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((true, (byte?)null)); // Exercises SQL ISNULL(...,1) branch

        if (string.Equals(licenseCategoryName, "CBEP", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((true, (byte?)1));

        return Task.FromResult((false, (byte?)null));
    }

    /// <inheritdoc/>
    public Task<(bool Found, byte? ProductPlatformId)> GetPartnerProductPlatformByCategoryAsync(
        int partnerId,
        string? siteId,
        string? licenseCategoryName,
        CancellationToken ct = default)
    {
        if (partnerId <= 0 || string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(licenseCategoryName))
            return Task.FromResult((false, (byte?)null));

        if (string.Equals(licenseCategoryName, "CBEP", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((true, (byte?)null)); // exercises ISNULL(...,1)

        return Task.FromResult((false, (byte?)null));
    }

    /// <inheritdoc/>
    public Task<(bool Found, byte? RetentionModelId)> GetPartnerRetentionModelByCategoryAsync(
        int partnerId,
        string? siteId,
        string? licenseCategoryName,
        CancellationToken ct = default)
    {
        if (partnerId <= 0 || string.IsNullOrWhiteSpace(siteId) || string.IsNullOrWhiteSpace(licenseCategoryName))
            return Task.FromResult((false, (byte?)null));

        if (string.Equals(licenseCategoryName, "OTSF", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((true, (byte?)null)); // exercises ISNULL(...,1)

        if (string.Equals(licenseCategoryName, "CBSB", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((true, (byte?)1));

        return Task.FromResult((false, (byte?)null));
    }

    /// <summary>Mock implementation of storage GB calculation.</summary>
    public Task<int?> GetItemStorageGbAsync(
        int quantity,
        string licenseCategoryName,
        byte? usagePricingModelId,
        CancellationToken ct = default)
    {
        // Mock: simple defaults based on usage model
        // Capacity model (2) = 100GB, others = 10GB
        int? result = usagePricingModelId == 2 ? 100 : 10;
        return Task.FromResult(result);
    }

    /// <summary>Mock implementation of product ID resolution using fn_product_select_profile.</summary>
    public Task<int?> ResolveProductIdAsync(
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
        // Mock: return a deterministic product ID based on inputs
        // In real scenario, fn_product_select_profile would determine this
        // For testing, use a simple hash/formula: productLineId * 1000 + licenseCategoryId
        var mockProductId = (productLineId * 1000) + licenseCategoryId;
        return Task.FromResult<int?>(mockProductId);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 2+: Write Methods (Mock implementations)
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <inheritdoc/>
    /// TODO: REPLACE WITH ACTUAL
    /// Real implementation must:
    ///   1. Begin a SQL transaction.
    ///   2. Call usp_cart_insert_cart_order(@site_id, @locale, @user_ip, @cart_extension_json,
    ///         @response_code OUTPUT, @message OUTPUT).
    ///      The @cart_extension_json blob contains: currency_code, vendor_order_code, partner_key,
    ///      account_user_name, routing_action, sales_order_date, message_campaign_id,
    ///      message_campaign_platform, key (message_key), cart_discount_id.
    ///   3. If response_code != 0 → throw / return error.
    ///   4. For each item in request.Items:
    ///        Build @item_json from CartOrderItemRequest fields.
    ///        Build @bundle_json (keycode, license_attribute_license_value,
    ///            license_keycode_type_id, order_item_update_type_id,
    ///            message_key, cart_discount_id, product_pricing_level_id).
    ///        Call usp_cart_insert_cart_order_item(@vendor_order_code, @item_json, @bundle_json,
    ///            @response_code OUTPUT, @message OUTPUT).
    ///   5. Commit transaction.
    ///   6. Return the vendor_order_code (output param from step 2 or supplied by caller).
    public Task<string> InsertCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default)
    {
        var id = Interlocked.Increment(ref _nextId);

        // TODO: REPLACE WITH ACTUAL — call usp_next_id(Type=3) + site prefix lookup table
        var vendorOrderCode = string.IsNullOrWhiteSpace(request.VendorOrderCode)
            ? $"MOCK-{id}"
            : request.VendorOrderCode;

        var now = DateTime.UtcNow;

        var response = new CartOrderResponse
        {
            CartOrderId = id,
            VendorOrderCode = vendorOrderCode,
            SiteId = request.SiteId,
            Locale = request.Locale,
            CurrencyCode = request.CurrencyCode ?? "USD", // TODO: REPLACE WITH ACTUAL — partner config fallback
            CurrencyId = 1,                               // TODO: REPLACE WITH ACTUAL — resolve from currency table
            CartOrderStatusId = 1,                        // TODO: REPLACE WITH ACTUAL — initial status from cart_order_status
            SalesOrderDate = request.SalesOrderDate?.Date ?? now.Date,
            InsertDate = now,
            InsertBy = "system",                          // TODO: REPLACE WITH ACTUAL — SUSER_SNAME() from SP
            UserIp = request.UserIp,
            PartnerKey = request.PartnerKey,
            CartJson = request.VendorOrderCode is not null ? null : null, // populated after real insert
            Items = request.Items.Select((item, i) => new CartOrderItemResponse
            {
                CartOrderItemId = id * 100 + i + 1,
                CartOrderId = id,
                LineItem = i + 1,
                ProductId = item.ProductId,
                LicenseCategoryName = item.LicenseCategoryName,
                Quantity = item.Quantity ?? 1,
                Seats = item.LicenseSeats,                // SP stores as both quantity and total_license_seats
                StorageGb = item.StorageGb,
                Years = item.Years,
                ItemHierarchyId = item.ItemHierarchyId,
                CartItemBundleId = item.CartItemBundleId,
                StartDate = item.StartDate,
                ExpirationDate = item.ExpirationDate,
                VendorOrderItemCode = item.VendorOrderItemCode,
                Discount = item.Discount,
                CartDiscountMethodId = item.CartDiscountMethodId,
                CartDiscountId = item.CartDiscountId,
                LicenseAttributeLicenseValue = item.LicenseAttributeLicenseValue,
                UsagePricingModelId = item.UsagePricingModelId,
                RetentionModelId = item.RetentionModelId,
                RetentionTerm = item.RetentionTerm,
                ProductPlatformId = item.ProductPlatformId,
                VaultId = item.VaultId,
                OpportunityLineItemId = item.OpportunityLineItemId,
                // Pricing fields below are NULL in mock — populated only from the real re-read SP
                // TODO: REPLACE WITH ACTUAL — these come from usp_cart_select_cart_order_item
                UnitPrice = null,
                ListPrice = null,
                UnitPricePreVat = null,
                TaxItemTotal = null,
                UsagePrice = null,
                OrderItemOfferAmount = null,
                EquivalentYearPrice = null,
                ProductPricingLevelId = item.ProductPricingLevelId
            }).ToList()
        };

        _store[vendorOrderCode] = response;
        return Task.FromResult(vendorOrderCode);
    }

    /// <inheritdoc/>
    /// TODO: REPLACE WITH ACTUAL
    /// Real implementation must call:
    ///   usp_cart_select_cart_order(@vendor_order_code) — returns header row
    ///     (cart_order JOIN cart_order_partner JOIN partner JOIN currency JOIN cart_json)
    ///   usp_cart_select_cart_order_item(@vendor_order_code) — returns all item rows with
    ///     full computed pricing, vault JSON, retention model, product platform, etc.
    public Task<CartOrderResponse?> SelectCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default)
    {
        _store.TryGetValue(vendorOrderCode, out var order);
        return Task.FromResult(order);
    }

    /// <inheritdoc/>
    public Task<CartOrderResponse?> SelectCartOrderByIdAsync(
        int cartOrderId,
        CancellationToken ct = default)
    {
        var order = _store.Values.FirstOrDefault(x => x.CartOrderId == cartOrderId);
        return Task.FromResult(order);
    }

    // ── Quote-key check ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// TODO: REPLACE WITH ACTUAL
    /// Real implementation must:
    ///   Query cart_order_message JOIN cart_order WHERE message_key = @key
    ///   AND cart_order_status = 'pending' (quote state).
    ///   Return vendor_order_code if found, else null.
    public Task<string?> FindExistingVendorOrderCodeByKeyAsync(
        string key, CancellationToken ct = default)
    {
        // Mock: no existing quote carts — always returns null (insert path)
        // TODO: REPLACE WITH ACTUAL — see description above
        return Task.FromResult<string?>(null);
    }

    // ── Read path ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// TODO: REPLACE WITH ACTUAL
    /// Real implementation must call:
    ///   usp_cart_select_message_key(@key) — resolves keycode → license record
    ///   usp_license_select_license_by_id(@license_id) — license details (seats, expiry, category)
    ///   usp_cart_select_license_profile(@license_id) — trial/full product profile
    ///   usp_cart_select_license_billing_model(@license_id) — billing model tooltip data
    public Task<LicenseOptionsResponse?> SelectLicenseOptionsAsync(
        string keycode, CancellationToken ct = default)
    {
        // TODO: REPLACE WITH ACTUAL — see description above
        var stub = new LicenseOptionsResponse
        {
            Keycode = keycode,
            LicenseStatus = "MOCK_ACTIVE",
            LicenseCategory = "MOCK_CATEGORY",
            LicenseSeats = 1,
            ExpirationDate = DateTime.UtcNow.AddYears(1),
            ProductOptions =
            [
                new ProductOptionResponse
                {
                    ProductId = 1,
                    ProductName = "Mock Product",
                    LicenseCategoryName = "MOCK",
                    Price = 9.99m,
                    Years = 1,
                    Seats = 1
                }
            ]
        };
        return Task.FromResult<LicenseOptionsResponse?>(stub);
    }

    /// <inheritdoc/>
    /// TODO: REPLACE WITH ACTUAL
    /// Real implementation must call:
    ///   usp_partner_cart_select_order_page_details (renewal context)
    ///   Returns: primary + secondary products, pricing, years, seats, storage options
    public Task<ConfigureResponse?> SelectConfigureAsync(
        string keycode, CancellationToken ct = default)
    {
        // TODO: REPLACE WITH ACTUAL — see description above
        var stub = new ConfigureResponse
        {
            Keycode = keycode,
            RenewalOptions =
            [
                new ProductOptionResponse
                {
                    ProductId = 10,
                    ProductName = "Mock Renewal",
                    Price = 49.99m,
                    Years = 1
                }
            ]
        };
        return Task.FromResult<ConfigureResponse?>(stub);
    }

    /// <inheritdoc/>
    /// TODO: REPLACE WITH ACTUAL
    /// Real implementation must call:
    ///   usp_product_select_license_category_upgrade
    ///   Returns: available upgrade product options for this license category
    public Task<UpgradeResponse?> SelectUpgradeAsync(
        string keycode, CancellationToken ct = default)
    {
        // TODO: REPLACE WITH ACTUAL — see description above
        var stub = new UpgradeResponse
        {
            Keycode = keycode,
            UpgradeOptions =
            [
                new ProductOptionResponse
                {
                    ProductId = 20,
                    ProductName = "Mock Upgrade",
                    Price = 99.99m,
                    Years = 1
                }
            ]
        };
        return Task.FromResult<UpgradeResponse?>(stub);
    }
}

