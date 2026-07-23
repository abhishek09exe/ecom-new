using System.Text.Json;
using System.Text.Json.Serialization;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Repositories;

namespace ecom_new_api.Services;

/// <summary>
/// SECTION 1: Preparation Service
/// 
/// Handles data loading and context preparation for cart order operations.
/// This is the foundation layer that loads reference data, validates lookups,
/// and prepares models for downstream processing.
/// 
/// IMPORTANT: This service does NOT contain:
/// - Pricing calculations
/// - Item insertion logic
/// - Business rule enforcement
/// - Data persistence
/// 
/// It ONLY loads, validates, and organizes existing data from the repository.
/// </summary>
public class CartOrderPreparationService
{
    private readonly ICartOrderRepository _repository;
    private readonly ILogger<CartOrderPreparationService> _logger;

    public CartOrderPreparationService(
        ICartOrderRepository repository,
        ILogger<CartOrderPreparationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Section 1 orchestration entry point.
    ///
    /// Executes the existing Section 1 preparation pipeline and returns a fully populated
    /// <see cref="CartOrderPreparedModel"/> for downstream Section 2 processing.
    /// </summary>
    /// <param name="request">Incoming cart order create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Prepared model containing context, license, items, product metadata, and fallbacks.</returns>
    public async Task<CartOrderPreparedModel> PrepareCartOrderAsync(
        CartOrderCreateRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogDebug("Starting Section 1 orchestration: PrepareCartOrderAsync");

        // 1) Context + account/currency resolution
        var userContext = LoadContextFromRequest(request);
        await ResolvePartnerIdAsync(userContext, ct);
        await ResolveCurrencyIdAsync(userContext, ct);
        await ApplyCurrencyFallbackAsync(userContext, ct);

        // 2) Build bundle/item contexts from request payload via existing Section 1 parsers
        var firstItemWithBundleData = request.Items.FirstOrDefault(i =>
            !string.IsNullOrWhiteSpace(i.Keycode)
            || i.LicenseAttributeLicenseValue.HasValue
            || i.LicenseKeycodeTypeId.HasValue
            || i.OrderItemUpdateTypeId.HasValue
            || i.ProductPricingLevelId.HasValue
            || i.CartDiscountId.HasValue);

        var bundlePayload = new BundleJsonPayload
        {
            Keycode = firstItemWithBundleData?.Keycode ?? request.Key,
            LicenseAttributeLicenseValue = firstItemWithBundleData?.LicenseAttributeLicenseValue,
            LicenseKeycodeTypeId = firstItemWithBundleData?.LicenseKeycodeTypeId,
            OrderItemUpdateTypeId = firstItemWithBundleData?.OrderItemUpdateTypeId ?? 1,
            ProductPricingLevelId = firstItemWithBundleData?.ProductPricingLevelId,
            CartDiscountId = firstItemWithBundleData?.CartDiscountId ?? request.CartDiscountId,
            MessageKey = request.Key
        };

        var bundleJson = JsonSerializer.Serialize(bundlePayload, BundleJsonPayload.SerializerOptions);
        var bundle = await ParseBundleJsonAsync(bundleJson, ct) ?? new BundleContext();
        if (bundle.LicenseAttributeLicenseValue.HasValue)
        {
            bundle.HasUtility = await IsUtilityBillingModelAsync(bundle.LicenseAttributeLicenseValue, ct);
        }

        var itemPayloads = request.Items.Select(i => new ItemJsonPayload
        {
            ProductId = i.ProductId,
            LicenseCategoryName = i.LicenseCategoryName,
            Quantity = i.Quantity ?? 1,
            LicenseSeats = i.LicenseSeats,
            StorageGb = i.StorageGb,
            Years = i.Years,
            StartDate = i.StartDate,
            ExpirationDate = i.ExpirationDate,
            VendorExpirationDate = i.VendorExpirationDate.HasValue
                ? i.VendorExpirationDate.Value.ToDateTime(TimeOnly.MinValue)
                : null,
            Keycode = i.Keycode,
            ItemHierarchyId = i.ItemHierarchyId,
            CartItemBundleId = i.CartItemBundleId,
            LicenseAttributeLicenseValue = i.LicenseAttributeLicenseValue,
            UsagePricingModelId = i.UsagePricingModelId,
            RetentionModelId = i.RetentionModelId,
            RetentionTerm = i.RetentionTerm,
            ProductPlatformId = i.ProductPlatformId,
            LicenseKeycodeTypeId = i.LicenseKeycodeTypeId,
            AmendedContract = i.AmendedContract,
        }).ToList();

        var itemJson = JsonSerializer.Serialize(itemPayloads, ItemJsonPayload.SerializerOptions);
        var items = await ParseItemJsonAsync(itemJson, ct);
        ValidateOtsfRetentionModel(items);

        // 3) Optional existing cart/item context
        Models.Responses.CartOrderResponse? cart = null;
        if (!string.IsNullOrWhiteSpace(userContext.VendorOrderCode))
        {
            cart = await LoadCartByVendorOrderCodeAsync(userContext.VendorOrderCode, ct);
        }

        var existingItems = await LoadExistingItemsAsync(cart, ct);

        // 4) License + locale + product metadata loading
        var licenseKeycode = bundle.Keycode
            ?? request.Key
            ?? request.Items.FirstOrDefault(i => !string.IsNullOrWhiteSpace(i.Keycode))?.Keycode;

        var license = await LoadLicenseByKeycodeAsync(licenseKeycode, ct);

        if (license is not null)
        {
            license.LicenseAttributeLicenseValueFromLicense = license.LicenseAttributeLicenseValue;
            license.LicenseAttributeLicenseValue = bundle.LicenseAttributeLicenseValue ?? license.LicenseAttributeLicenseValue;
            license.LicenseKeycodeTypeId = ApplyLicenseKeycodeTypeFallback(bundle, license);
        }

        var products = await PrepareProductsFromItemsAsync(items, ct);
        EnrichItemsWithLicenseCategoryId(items, products, license);
        NormalizeDownRevCategoryNames(items, license);

        var (languageCode, locationCode) = await LoadLocaleCodesByLocaleAsync(userContext.Locale, ct);

        int? productLineId = null;
        var firstLicenseCategoryId = items.Select(i => i.LicenseCategoryId).FirstOrDefault(x => x.HasValue);
        if (firstLicenseCategoryId.HasValue)
        {
            productLineId = await LoadProductLineByLicenseCategoryAsync(
                firstLicenseCategoryId.Value,
                languageCode,
                locationCode,
                ct);
        }

        if (!productLineId.HasValue)
        {
            productLineId = products.Values
                .Select(p => p.ProductLineId)
                .FirstOrDefault(x => x.HasValue);
        }

        productLineId = RemapProductLineId(productLineId);

        // 5) Billing-model and attribute fallback chain
        int? globalBillingModelId = bundle.LicenseAttributeLicenseValue;
        int? globalLicenseAttributeId = await ResolveLicenseAttributeIdAsync(globalBillingModelId, ct);

        var fallbackSeed = new CartOrderPreparedModel
        {
            ExistingItems = existingItems
        };

        (globalBillingModelId, globalLicenseAttributeId) = ApplyGlobalBillingModelFallback(
            fallbackSeed,
            globalBillingModelId,
            globalLicenseAttributeId);

        globalBillingModelId = ApplyBusinessBillingModelFallback(productLineId, globalBillingModelId);

        var licenseCategoryIds = items
            .Where(i => i.LicenseCategoryId.HasValue)
            .Select(i => i.LicenseCategoryId!.Value)
            .Distinct()
            .ToList();

        (globalBillingModelId, globalLicenseAttributeId) = await ApplyLocationBasedBillingModelAsync(
            productLineId,
            locationCode,
            licenseCategoryIds,
            globalBillingModelId,
            globalLicenseAttributeId,
            ct);

        // 6) Item-level enrichments
        EnrichBillingModelFallback(items, ct);
        EnrichUsagePricingModel(items, license);
        EnrichRetentionModel(items, license);
        EnrichProductPlatform(items, license);

        var sfdcOverrides = BuildSfdcUnitOverrides(userContext.SiteId, items, license);
        CalculateUpgradeLicenseSeats(items, license, sfdcOverrides);
        ApplyMonthlyToAnnualConversion(items, license, bundle.LicenseAttributeLicenseValue, nextProcessDate: null);
        await EnrichDefaultStorageGbWithRepositoryAsync(items, ct);

        // 7) Final aggregate model
        var preparedModel = AssemblePreparedModel(cart, userContext, license, items, products);
        preparedModel.ExistingItems = existingItems;
        preparedModel.SiteId = userContext.SiteId;
        preparedModel.ProductLineId = productLineId;
        preparedModel.BillingModelId = bundle.LicenseAttributeLicenseValue;
        preparedModel.GlobalBillingModelId = globalBillingModelId;
        preparedModel.LanguageCode = languageCode;
        preparedModel.LocationCode = locationCode;
        preparedModel.LicenseId = license?.LicenseId;
        preparedModel.NextProcessDate = null;
        preparedModel.PartnerId = userContext.PartnerId;
        preparedModel.HasUtility = bundle.HasUtility;

        _logger.LogInformation(
            "PrepareCartOrderAsync complete: Items={ItemCount}, ExistingItems={ExistingCount}, ProductLine={ProductLine}, BillingModel={BillingModel}",
            preparedModel.Items.Count,
            preparedModel.ExistingItems.Count,
            preparedModel.ProductLineId,
            preparedModel.GlobalBillingModelId);

        return preparedModel;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.1: Load Cart
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.1: Load existing cart by vendor order code
    /// 
    /// Purpose: Retrieve the full cart aggregate (header + items + partner info)
    /// for an existing order when the code is known.
    /// 
    /// Database Query:
    ///   SELECT * FROM cart_order WHERE vendor_order_code = @code
    ///   SELECT * FROM cart_order_item WHERE cart_order_id = @order_id
    ///   SELECT * FROM cart_order_partner WHERE cart_order_id = @order_id
    ///   SELECT * FROM cart_json WHERE cart_order_id = @order_id (optional)
    /// 
    /// Returns: Null if cart doesn't exist, full CartOrderResponse if found
    /// </summary>
    public async Task<Models.Responses.CartOrderResponse?> LoadCartByVendorOrderCodeAsync(
        string vendorOrderCode,
        CancellationToken ct = default)
    {
        // Validation: null/empty code
        if (string.IsNullOrWhiteSpace(vendorOrderCode))
        {
            _logger.LogWarning("LoadCartByVendorOrderCodeAsync called with null/empty code");
            return null;
        }

        _logger.LogDebug("Loading cart by vendor order code: {VendorOrderCode}", vendorOrderCode);

        // Query repository (maps to usp_cart_select_cart_order + items)
        var cart = await _repository.SelectCartOrderAsync(vendorOrderCode, ct);

        if (cart is null)
        {
            _logger.LogWarning("Cart not found for code: {VendorOrderCode}", vendorOrderCode);
            return null;
        }

        _logger.LogInformation(
            "Cart loaded: VendorOrderCode={Code}, Items={ItemCount}, Partner={Partner}",
            cart.VendorOrderCode, cart.Items?.Count ?? 0, cart.PartnerKey ?? "none");

        return cart;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.2: Load Context
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.2: Load and validate user context from request
    /// 
    /// Purpose: Extract and normalize user/account context information that will
    /// be needed for downstream processing (lookups, auditing, pricing by account).
    /// 
    /// This does NOT perform authentication — it merely organizes the context
    /// that middleware has already injected or that the client provided.
    /// 
    /// Returns: Prepared context model with all fields normalized
    /// </summary>
    public CartOrderUserContext LoadContextFromRequest(CartOrderCreateRequest request)
    {
        // Validation
        if (request is null)
        {
            _logger.LogWarning("LoadContextFromRequest called with null request");
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogDebug("Loading user context from request: SiteId={SiteId}, Locale={Locale}",
            request.SiteId, request.Locale);

        // Build context model
        var context = new CartOrderUserContext
        {
            // Order-level context
            SiteId = request.SiteId?.Trim() ?? "UNKNOWN",
            Locale = request.Locale?.Trim() ?? "en_US",
            UserIp = request.UserIp?.Trim() ?? "0.0.0.0",

            // Account context (injected by middleware)
            CsiUserId = request.CsiUserId,
            AccountUserName = request.AccountUserName?.Trim(),
            PartnerRateCode = request.PRc?.Trim(),
            TrxRc = request.TrxRc?.Trim(),

            // Optional fields
            PartnerKey = request.PartnerKey?.Trim(),
            CurrencyCode = request.CurrencyCode?.Trim()?.ToUpper(),
            VendorOrderCode = request.VendorOrderCode?.Trim(),
            RoutingAction = request.RoutingAction?.Trim(),
            UrlLink = request.UrlLink?.Trim(),

            // Campaign context
            MessageCampaignId = request.MessageCampaignId,
            MessageCampaignPlatform = request.MessageCampaignPlatform?.Trim(),
            Key = request.Key?.Trim(),
            CartDiscountId = request.CartDiscountId,

            // Timestamp context
            SalesOrderDate = request.SalesOrderDate?.Date ?? DateTime.UtcNow.Date,
            LoadedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Context loaded: Site={Site}, User={User}, Partner={Partner}, Currency={Currency}",
            context.SiteId, context.CsiUserId, context.PartnerKey ?? "none", 
            context.CurrencyCode ?? "default");

        return context;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.3 (JSON): Parse Bundle JSON
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.3: Parse @bundle_json into a strongly-typed <see cref="BundleContext"/>.
    ///
    /// Purpose: Extract per-order bundle metadata supplied by the caller as a JSON blob.
    /// This is the C# equivalent of the SQL OPENJSON(@bundle_json) pattern used inside
    /// <c>usp_cart_insert_cart_order_item</c>.  No database calls are made here.
    ///
    /// Extracted fields (matching SQL column names):
    /// <list type="bullet">
    ///   <item><c>keycode</c> – license keycode for the bundle.</item>
    ///   <item><c>license_attribute_license_value</c> – billing model code (e.g., 110, 111).</item>
    ///   <item><c>license_keycode_type_id</c> – keycode type discriminator.</item>
    ///   <item><c>order_item_update_type_id</c> – operation selector (1=insert, 2=update …).</item>
    ///   <item><c>product_pricing_level_id</c> – pricing tier for bundle products.</item>
    ///   <item><c>cart_discount_id</c> – optional discount to apply to the order.</item>
    ///   <item><c>key</c> – message key / license lookup key.</item>
    /// </list>
    ///
    /// Returns <see langword="null"/> when <paramref name="bundleJson"/> is null or whitespace.
    /// Returns an empty <see cref="BundleContext"/> when the JSON is non-null but contains
    /// no recognised fields (caller must handle this gracefully).
    /// </summary>
    /// <param name="bundleJson">
    ///   Raw JSON string from the <c>@bundle_json</c> parameter.
    ///   Example: <c>{"keycode":"ABC123","license_attribute_license_value":110}</c>
    /// </param>
    /// <param name="ct">Cancellation token (reserved for future async use).</param>
    /// <returns>A populated <see cref="BundleContext"/>, or <see langword="null"/> if input is empty.</returns>
    public async Task<BundleContext?> ParseBundleJsonAsync(
        string? bundleJson,
        CancellationToken ct = default)
    {
        // Early return: null/empty JSON
        if (string.IsNullOrWhiteSpace(bundleJson))
        {
            _logger.LogDebug("ParseBundleJsonAsync: bundleJson is null or empty – returning null");
            return null;
        }

        _logger.LogDebug("ParseBundleJsonAsync: parsing bundle JSON ({Length} chars)", bundleJson.Length);

        try
        {
            var raw = JsonSerializer.Deserialize<BundleJsonPayload>(
                bundleJson,
                BundleJsonPayload.SerializerOptions);

            if (raw is null)
            {
                _logger.LogWarning("ParseBundleJsonAsync: JSON deserialized to null – returning empty context");
                return new BundleContext();
            }

            // Normalize empty strings to null (mirrors SQL: CASE WHEN x = '' THEN null ELSE x END)
            var context = new BundleContext
            {
                Keycode                        = NullIfEmpty(raw.Keycode),
                LicenseAttributeLicenseValue   = raw.LicenseAttributeLicenseValue,
                LicenseKeycodeTypeId           = raw.LicenseKeycodeTypeId,
                OrderItemUpdateTypeId          = raw.OrderItemUpdateTypeId ?? 1,  // SQL: ISNULL(@order_item_update_type_id, 1) - default to Insert
                ProductPricingLevelId          = raw.ProductPricingLevelId,
                CartDiscountId                 = raw.CartDiscountId,
                MessageKey                     = NullIfEmpty(raw.MessageKey),
            };

            // SECTION 1.3: Detect utility billing model (SQL: IF EXISTS ... SELECT @has_utility = 1)
            if (context.LicenseAttributeLicenseValue.HasValue)
            {
                context.HasUtility = await IsUtilityBillingModelAsync(
                    context.LicenseAttributeLicenseValue,
                    ct);
            }

            _logger.LogDebug(
                "ParseBundleJsonAsync: parsed – Keycode={Keycode}, BillingModel={BillingModel}, " +
                "HasUtility={HasUtility}, UpdateType={UpdateType}, PricingLevel={PricingLevel}",
                context.Keycode ?? "(none)",
                context.LicenseAttributeLicenseValue?.ToString() ?? "(none)",
                context.HasUtility ? "YES" : "NO",
                context.OrderItemUpdateTypeId.ToString(),
                context.ProductPricingLevelId?.ToString() ?? "(none)");

            return context;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "ParseBundleJsonAsync: malformed JSON – returning null. JSON snippet: {Snippet}",
                bundleJson.Length > 200 ? bundleJson[..200] : bundleJson);

            return null;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.4 (JSON): Parse Item JSON Array
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.4: Parse <c>@item_json</c> array into a list of <see cref="CartOrderItemContext"/> objects.
    ///
    /// Purpose: Extract the collection of new line-items supplied by the caller as a JSON array.
    /// This is the C# equivalent of the SQL OPENJSON(@item_json) WITH (…) pattern inside
    /// <c>usp_cart_insert_cart_order_item</c>.  No database calls are made here.
    ///
    /// Populated fields per item:
    /// <list type="bullet">
    ///   <item><c>product_id</c></item>
    ///   <item><c>license_category_name</c></item>
    ///   <item><c>quantity</c></item>
    ///   <item><c>license_seats</c></item>
    ///   <item><c>storage_gb</c></item>
    ///   <item><c>years</c></item>
    ///   <item><c>item_hierarchy_id</c></item>
    ///   <item><c>cart_item_bundle_id</c></item>
    ///   <item><c>license_attribute_license_value</c></item>
    ///   <item><c>usage_pricing_model_id</c></item>
    ///   <item><c>retention_model_id</c></item>
    ///   <item><c>retention_term</c></item>
    ///   <item><c>product_platform_id</c></item>
    ///   <item><c>start_date</c></item>
    ///   <item><c>expiration_date</c></item>
    ///   <item><c>keycode</c></item>
    /// </list>
    ///
    /// Items with <c>product_id = 0</c> are skipped (mirrors SQL behaviour where unresolvable
    /// product rows are ignored).
    /// </summary>
    /// <param name="itemJson">
    ///   Raw JSON array string from the <c>@item_json</c> parameter.
    ///   Example: <c>[{"product_id":42,"quantity":1,"years":1}]</c>
    /// </param>
    /// <param name="ct">Cancellation token (reserved for future async use).</param>
    /// <returns>
    ///   List of populated <see cref="CartOrderItemContext"/> objects (never null; empty on failure).
    /// </returns>
    public Task<List<CartOrderItemContext>> ParseItemJsonAsync(
        string? itemJson,
        CancellationToken ct = default)
    {
        var result = new List<CartOrderItemContext>();

        // Early return: null/empty JSON
        if (string.IsNullOrWhiteSpace(itemJson))
        {
            _logger.LogDebug("ParseItemJsonAsync: itemJson is null or empty – returning empty list");
            return Task.FromResult(result);
        }

        _logger.LogDebug("ParseItemJsonAsync: parsing item JSON array ({Length} chars)", itemJson.Length);

        try
        {
            var rawItems = JsonSerializer.Deserialize<List<ItemJsonPayload>>(
                itemJson,
                ItemJsonPayload.SerializerOptions);

            if (rawItems is null || rawItems.Count == 0)
            {
                _logger.LogWarning("ParseItemJsonAsync: JSON produced no items");
                return Task.FromResult(result);
            }

            for (int i = 0; i < rawItems.Count; i++)
            {
                var raw = rawItems[i];

                // Skip items with no product ID (mirrors SQL implicit filter)
                if (raw.ProductId <= 0)
                {
                    _logger.LogDebug("ParseItemJsonAsync: skipping item[{Index}] – ProductId is 0 or missing", i);
                    continue;
                }

                var context = new CartOrderItemContext
                {
                    // CartOrderItemId / CartOrderId are 0 for NEW items (not yet inserted)
                    CartOrderItemId  = 0,
                    CartOrderId      = 0,
                    LineItem         = i + 1,

                    ProductId            = raw.ProductId,
                    LicenseCategoryName  = NullIfEmpty(raw.LicenseCategoryName),
                    Quantity             = raw.Quantity > 0 ? raw.Quantity : 1,
                    LicenseSeats         = raw.LicenseSeats,
                    TotalLicenseSeats    = raw.LicenseSeats,  // SQL: total_license_seats = license_seats (initialized from input)
                    StorageGb            = raw.StorageGb,
                    Years                = raw.Years,
                    StartDate            = raw.StartDate,
                    ExpirationDate       = raw.ExpirationDate,
                    VendorExpirationDate = raw.VendorExpirationDate,
                    Keycode              = NullIfEmpty(raw.Keycode),

                    ItemHierarchyId              = raw.ItemHierarchyId,
                    CartItemBundleId             = raw.CartItemBundleId,
                    LicenseAttributeLicenseValue = raw.LicenseAttributeLicenseValue,
                    UsagePricingModelId          = raw.UsagePricingModelId,
                    RetentionModelId             = raw.RetentionModelId,
                    RetentionTerm                = raw.RetentionTerm,
                    ProductPlatformId            = raw.ProductPlatformId,
                    LicenseKeycodeTypeId         = raw.LicenseKeycodeTypeId,
                    AmendedContract              = NullIfEmpty(raw.AmendedContract),  // SQL: CASE WHEN amended_contract = '' THEN NULL ELSE amended_contract END

                    LoadedAt = DateTime.UtcNow
                };

                result.Add(context);

                _logger.LogDebug(
                    "ParseItemJsonAsync: item[{Index}] – ProductId={ProductId}, " +
                    "Category={Category}, Qty={Qty}, Seats={Seats}, Hierarchy={Hierarchy}",
                    i, context.ProductId, context.LicenseCategoryName ?? "(none)",
                    context.Quantity, context.LicenseSeats, context.ItemHierarchyId);
            }

            _logger.LogInformation(
                "ParseItemJsonAsync: parsed {Parsed} items from {Total} raw entries",
                result.Count, rawItems.Count);

            return Task.FromResult(result);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "ParseItemJsonAsync: malformed JSON – returning empty list. JSON snippet: {Snippet}",
                itemJson.Length > 200 ? itemJson[..200] : itemJson);

            return Task.FromResult(result);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // Private helpers
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>Returns <see langword="null"/> when <paramref name="value"/> is null or whitespace.</summary>
    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.2: Resolve Partner ID
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.2: Resolve <c>partner_id</c> from a <c>partner_key</c> GUID string.
    ///
    /// Purpose: Translate the caller-supplied <c>PartnerKey</c> (a GUID) into the internal
    /// integer <c>partner_id</c> that is stored on <c>cart_order_partner</c>.  The resolved
    /// value is written back onto <see cref="CartOrderUserContext.PartnerId"/>; the original
    /// <see cref="CartOrderUserContext.PartnerKey"/> is always preserved.
    ///
    /// SQL equivalent: <c>SELECT partner_id FROM partner WHERE partner_key = @partner_key</c>
    ///
    /// Non-fatal: a missing or unrecognised key logs a warning and leaves <c>PartnerId = null</c>.
    /// </summary>
    /// <param name="context">Context object to mutate; <c>PartnerId</c> is set in-place.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task ResolvePartnerIdAsync(
        CartOrderUserContext context,
        CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        if (string.IsNullOrWhiteSpace(context.PartnerKey))
        {
            _logger.LogDebug("ResolvePartnerIdAsync: PartnerKey is absent – skipping partner lookup");
            return;
        }

        _logger.LogDebug("ResolvePartnerIdAsync: looking up partner for key {PartnerKey}", context.PartnerKey);

        try
        {
            var partnerId = await _repository.LookupPartnerIdByKeyAsync(context.PartnerKey, ct);

            if (partnerId.HasValue)
            {
                context.PartnerId = partnerId.Value;
                _logger.LogDebug(
                    "ResolvePartnerIdAsync: resolved PartnerKey={Key} → PartnerId={Id}",
                    context.PartnerKey, partnerId.Value);
            }
            else
            {
                _logger.LogWarning(
                    "ResolvePartnerIdAsync: partner not found for key {PartnerKey} – continuing without partner",
                    context.PartnerKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ResolvePartnerIdAsync: error looking up partner for key {PartnerKey} – continuing without partner",
                context.PartnerKey);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.3: Resolve Currency ID
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.3: Resolve <c>currency_id</c> from <c>currency_code</c>, with partner
    /// configuration and hard-coded USD fallbacks.
    ///
    /// Resolution order (mirrors SQL usp_cart_insert_cart_order):
    /// <list type="number">
    ///   <item>Look up <c>currency_id</c> by <c>CurrencyCode</c> directly from the <c>currency</c> table.</item>
    ///   <item>If not found and <c>PartnerId</c> is known, query <c>partner_configuration_partner</c>
    ///         (configuration_id = 15) for the partner's preferred currency.</item>
    ///   <item>If still not resolved, default to <c>currency_id = 1</c> (USD) — the same hard default
    ///         used in the SQL procedure.</item>
    /// </list>
    ///
    /// The resolved value is written back onto <see cref="CartOrderUserContext.CurrencyId"/>.
    /// Non-fatal: fallback warnings are logged instead of throwing.
    /// </summary>
    /// <param name="context">Context object to mutate; <c>CurrencyId</c> is set in-place.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task ResolveCurrencyIdAsync(
        CartOrderUserContext context,
        CancellationToken ct = default)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        const int defaultCurrencyId = 1; // USD – mirrors SQL procedure default

        // ── Step 1: resolve from CurrencyCode ────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(context.CurrencyCode))
        {
            _logger.LogDebug(
                "ResolveCurrencyIdAsync: looking up currency for code {CurrencyCode}",
                context.CurrencyCode);

            try
            {
                var currencyId = await _repository.LookupCurrencyIdByCodeAsync(context.CurrencyCode, ct);

                if (currencyId.HasValue)
                {
                    context.CurrencyId = currencyId.Value;
                    _logger.LogDebug(
                        "ResolveCurrencyIdAsync: resolved CurrencyCode={Code} → CurrencyId={Id}",
                        context.CurrencyCode, currencyId.Value);
                    return;
                }

                _logger.LogWarning(
                    "ResolveCurrencyIdAsync: currency code {CurrencyCode} not found in currency table",
                    context.CurrencyCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "ResolveCurrencyIdAsync: error looking up currency code {CurrencyCode}",
                    context.CurrencyCode);
            }
        }
        else
        {
            _logger.LogDebug("ResolveCurrencyIdAsync: CurrencyCode is absent – skipping direct lookup");
        }

        // ── Step 2: partner configuration fallback (partner_configuration_id = 15) ──
        // This mirrors the SQL block:
        //   IF @currency_id IS NULL AND @partner_id IS NOT NULL
        //     SELECT @currency_code = c.currency_code, @currency_id = c.currency_id
        //     FROM partner_configuration_partner cp
        //     INNER JOIN currency c ON cp.configuration_value = c.currency_code
        //     WHERE cp.partner_id = @partner_id AND cp.partner_configuration_id = 15
        //
        // NOTE: The repository does not yet expose partner-configuration-currency lookup.
        //       When it does, call it here with context.PartnerId.
        //       For now we log and fall through to the default.
        if (context.PartnerId.HasValue)
        {
            _logger.LogDebug(
                "ResolveCurrencyIdAsync: partner currency fallback for PartnerId={PartnerId} – " +
                "partner_configuration lookup not yet implemented; falling through to default",
                context.PartnerId);
        }

        // ── Step 3: hard default (USD = 1) ───────────────────────────────────────────
        context.CurrencyId = defaultCurrencyId;
        _logger.LogDebug(
            "ResolveCurrencyIdAsync: applying default CurrencyId={Default} (USD)",
            defaultCurrencyId);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.4.2: Enrich Items with LicenseCategoryId
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.4.2: Populate <see cref="CartOrderItemContext.LicenseCategoryId"/> on each item.
    ///
    /// Purpose: The SQL procedure enriches each item row with <c>license_category_id</c> after
    /// the initial item population.  This method does the same without hitting the database by
    /// reading from already-loaded in-memory data:
    /// <list type="number">
    ///   <item>From the <paramref name="products"/> dictionary (product → license_category_id).</item>
    ///   <item>From <paramref name="license"/> when the item's <c>LicenseCategoryName</c> matches
    ///         the license's category name.</item>
    /// </list>
    ///
    /// No database calls are made.  Items for which no match is found are left with
    /// <c>LicenseCategoryId = null</c> and a debug log is emitted.
    /// </summary>
    /// <param name="items">Item list to enrich in-place.</param>
    /// <param name="products">Product dictionary loaded in Section 1.5.</param>
    /// <param name="license">License context loaded in Section 1.3 (may be null).</param>
    public void EnrichItemsWithLicenseCategoryId(
        List<CartOrderItemContext> items,
        Dictionary<int, CartOrderProductContext> products,
        CartOrderLicenseContext? license)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("EnrichItemsWithLicenseCategoryId: no items to enrich");
            return;
        }

        int enriched = 0;
        int notFound = 0;

        foreach (var item in items)
        {
            // ── Source 1: product catalog (preferred – most specific per product) ──
            if (products.TryGetValue(item.ProductId, out var product)
                && product.LicenseCategoryId.HasValue)
            {
                item.LicenseCategoryId = product.LicenseCategoryId;
                enriched++;

                _logger.LogDebug(
                    "EnrichItemsWithLicenseCategoryId: item LineItem={Line} ProductId={Product} " +
                    "→ LicenseCategoryId={CategoryId} (from product)",
                    item.LineItem, item.ProductId, item.LicenseCategoryId);
                continue;
            }

            // ── Source 2: license context (same category name) ─────────────────────
            if (license is not null
                && !string.IsNullOrWhiteSpace(item.LicenseCategoryName)
                && string.Equals(item.LicenseCategoryName, license.LicenseCategoryName,
                    StringComparison.OrdinalIgnoreCase)
                && license.LicenseId > 0)
            {
                // LicenseCategoryId is not directly stored on CartOrderLicenseContext;
                // we encode the contract: LicenseId > 0 implies category was loaded.
                // The actual int is unavailable here without an extra field.
                // Leave null and let the caller supply it from LicenseEntity if needed.
                _logger.LogDebug(
                    "EnrichItemsWithLicenseCategoryId: item LineItem={Line} matches license category " +
                    "{CategoryName} but LicenseCategoryId not available on context – skipping",
                    item.LineItem, item.LicenseCategoryName);
                notFound++;
                continue;
            }

            // ── No source resolved ─────────────────────────────────────────────────
            _logger.LogDebug(
                "EnrichItemsWithLicenseCategoryId: item LineItem={Line} ProductId={Product} " +
                "– LicenseCategoryId not resolved (product not in catalog)",
                item.LineItem, item.ProductId);
            notFound++;
        }

        _logger.LogInformation(
            "EnrichItemsWithLicenseCategoryId: {Enriched} enriched, {NotFound} unresolved out of {Total} items",
            enriched, notFound, items.Count);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.3: Detect Utility Billing Models
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.3: Detect whether a billing model belongs to the utility billing models set.
    ///
    /// SQL equivalent:
    /// <code>
    /// IF EXISTS (SELECT 1 FROM @UTILITY_BILLING_MODELS WHERE license_attribute_license_value = @value)
    ///   SET @has_utility = 1
    /// </code>
    ///
    /// Non-fatal: logging only. Returns false if lookup fails or value is null.
    /// </summary>
    /// <param name="billingModelId">The billing model ID to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the billing model is a utility model; false otherwise.</returns>
    public async Task<bool> IsUtilityBillingModelAsync(int? billingModelId, CancellationToken ct = default)
    {
        if (!billingModelId.HasValue || billingModelId.Value <= 0)
        {
            _logger.LogDebug("IsUtilityBillingModelAsync: billing model ID is null or invalid");
            return false;
        }

        _logger.LogDebug("IsUtilityBillingModelAsync: checking billing model {BillingModelId}", billingModelId);

        try
        {
            var isUtility = await _repository.IsUtilityBillingModelAsync(billingModelId.Value, ct);

            if (isUtility)
                _logger.LogDebug("IsUtilityBillingModelAsync: billing model {Id} is a utility model", billingModelId);

            return isUtility;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "IsUtilityBillingModelAsync: error checking billing model {Id} – returning false",
                billingModelId);
            return false;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.3.2: Resolve License Attribute ID
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.3.2: Resolve <c>license_attribute_id</c> from <c>license_attribute_license_value</c>.
    ///
    /// SQL equivalent:
    /// <code>
    /// SELECT @license_attribute_id = license_attribute_id
    /// FROM dbo.license_attribute_license_value
    /// WHERE license_attribute_license_value = @value
    /// </code>
    ///
    /// Non-fatal: returns null if not found and logs a warning.
    /// </summary>
    /// <param name="billingModelId">The billing model ID (license_attribute_license_value).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The license_attribute_id, or null if not found.</returns>
    public async Task<int?> ResolveLicenseAttributeIdAsync(int? billingModelId, CancellationToken ct = default)
    {
        if (!billingModelId.HasValue || billingModelId.Value <= 0)
        {
            _logger.LogDebug("ResolveLicenseAttributeIdAsync: billing model ID is null or invalid");
            return null;
        }

        _logger.LogDebug(
            "ResolveLicenseAttributeIdAsync: resolving attribute ID for billing model {BillingModelId}",
            billingModelId);

        try
        {
            var attributeId = await _repository.GetLicenseAttributeIdByValueAsync(billingModelId.Value, ct);

            if (attributeId.HasValue)
                _logger.LogDebug(
                    "ResolveLicenseAttributeIdAsync: billing model {BillingModel} → attribute ID {AttrId}",
                    billingModelId, attributeId);
            else
                _logger.LogWarning(
                    "ResolveLicenseAttributeIdAsync: no attribute ID found for billing model {BillingModel}",
                    billingModelId);

            return attributeId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ResolveLicenseAttributeIdAsync: error resolving attribute ID for billing model {BillingModel}",
                billingModelId);
            return null;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.9.2: Product Line Remapping
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.9.2: Remap legacy product line IDs to canonical values.
    ///
    /// SQL equivalent:
    /// <code>
    /// IF @product_line_id IN (1, 6)
    ///   SELECT @product_line_id = CASE
    ///     WHEN @product_line_id = 1 THEN 100
    ///     WHEN @product_line_id = 6 THEN 200
    ///   END
    /// </code>
    ///
    /// Non-fatal: returns the input if not in the remap set.
    /// </summary>
    /// <param name="productLineId">The product line ID to remap.</param>
    /// <returns>The remapped product line ID (1→100, 6→200, others unchanged).</returns>
    public int? RemapProductLineId(int? productLineId)
    {
        if (!productLineId.HasValue || productLineId.Value <= 0)
            return productLineId;

        var remapped = productLineId.Value switch
        {
            1 => 100,
            6 => 200,
            _ => productLineId.Value
        };

        if (remapped != productLineId.Value)
            _logger.LogDebug(
                "RemapProductLineId: product line {OldId} → {NewId}",
                productLineId, remapped);

        return remapped;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.10: Down-Rev Category Name Normalization
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.10: Normalize down-rev category names to canonical names.
    ///
    /// SQL equivalent:
    /// <code>
    /// UPDATE @item_table
    /// SET license_category_name = CASE
    ///   WHEN license_category_name = 'AD' THEN 'ADP'
    ///   WHEN license_category_name IN ('WAV', 'SS') THEN 'WSAV'
    ///   WHEN license_category_name IN ('WISE', 'WSAE') THEN 'WSAI'
    ///   WHEN license_category_name = 'WISC' THEN 'WSAC'
    /// END
    /// WHERE license_category_name IN ('SS', 'WAV', 'WISC', 'WISE', 'WSAE', 'AD')
    /// </code>
    ///
    /// Normalizes both item categories and license categories for downstream comparisons.
    /// No-op if category is already canonical.
    /// </summary>
    /// <param name="items">Items to normalize in-place.</param>
    /// <param name="license">License context to normalize in-place (may be null).</param>
    public void NormalizeDownRevCategoryNames(
        List<CartOrderItemContext> items,
        CartOrderLicenseContext? license)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("NormalizeDownRevCategoryNames: no items to normalize");
            return;
        }

        string NormalizeCategory(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return categoryName ?? string.Empty;

            return categoryName switch
            {
                "AD" => "ADP",
                "WAV" or "SS" => "WSAV",
                "WISE" or "WSAE" => "WSAI",
                "WISC" => "WSAC",
                _ => categoryName
            };
        }

        int itemsUpdated = 0;

        foreach (var item in items)
        {
            var normalized = NormalizeCategory(item.LicenseCategoryName);
            if (normalized != item.LicenseCategoryName)
            {
                _logger.LogDebug(
                    "NormalizeDownRevCategoryNames: LineItem={Line} {OldName} → {NewName}",
                    item.LineItem, item.LicenseCategoryName, normalized);
                item.LicenseCategoryName = normalized;
                itemsUpdated++;
            }
        }

        if (license is not null)
        {
            var normalized = NormalizeCategory(license.LicenseCategoryName);
            if (normalized != license.LicenseCategoryName)
            {
                _logger.LogDebug(
                    "NormalizeDownRevCategoryNames: license category {OldName} → {NewName}",
                    license.LicenseCategoryName, normalized);
                license.LicenseCategoryName = normalized;
            }
        }

        _logger.LogInformation(
            "NormalizeDownRevCategoryNames: normalized {ItemsUpdated} items",
            itemsUpdated);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.3.1: License Keycode Type Fallback Logic
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.3.1: Apply fallback logic for license keycode type ID.
    ///
    /// SQL equivalent:
    /// <code>
    /// SELECT @license_keycode_type_id = CASE
    ///   WHEN @license_keycode_type_id IS NULL THEN license_keycode_type_id
    ///   ELSE @license_keycode_type_id
    /// END
    /// </code>
    ///
    /// Logic: If bundle provides a keycode type, use it. Otherwise, use the license's value.
    /// This allows bundle overrides while preserving license defaults.
    /// </summary>
    /// <param name="bundle">Bundle context (may have LicenseKeycodeTypeId).</param>
    /// <param name="license">License context (has LicenseKeycodeTypeId).</param>
    /// <returns>The resolved license keycode type ID (bundle value preferred over license value).</returns>
    public int? ApplyLicenseKeycodeTypeFallback(BundleContext? bundle, CartOrderLicenseContext? license)
    {
        if (bundle is null || license is null)
            return license?.LicenseKeycodeTypeId;

        // Preserve bundle's value if provided, otherwise use license's value
        var resolved = bundle.LicenseKeycodeTypeId ?? license.LicenseKeycodeTypeId;

        if (bundle.LicenseKeycodeTypeId.HasValue && bundle.LicenseKeycodeTypeId != license.LicenseKeycodeTypeId)
            _logger.LogDebug(
                "ApplyLicenseKeycodeTypeFallback: using bundle override {BundleValue} instead of license {LicenseValue}",
                bundle.LicenseKeycodeTypeId, license.LicenseKeycodeTypeId);
        else if (!bundle.LicenseKeycodeTypeId.HasValue && license.LicenseKeycodeTypeId.HasValue)
            _logger.LogDebug(
                "ApplyLicenseKeycodeTypeFallback: using license value {LicenseValue}",
                license.LicenseKeycodeTypeId);

        return resolved;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.4.2: Business Billing Model Fallback
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.4.2: Resolve business billing model with fallback logic.
    ///
    /// SQL equivalent (simplified):
    /// <code>
    /// IF @product_line_id IN (SELECT [key] FROM fn_app_config_select_key_values('BUSINESS_PRODUCT_LINE', 'GENERAL'))
    /// BEGIN
    ///   IF @license_attribute_license_value IS NULL
    ///     SELECT @license_attribute_license_value = default_billing_model FROM DEFAULT_BUSINESS_BILLING_MODEL
    /// END
    /// </code>
    ///
    /// Logic: If the product line is a BUSINESS line and billing model is null/default,
    /// resolve the correct business billing model from configuration.
    ///
    /// Non-fatal: Returns the input value if not a business product line, or if lookup fails.
    /// </summary>
    /// <param name="productLineId">The product line ID to check.</param>
    /// <param name="billingModelId">The current billing model (may be null).</param>
    /// <returns>The resolved billing model ID (or input if not a business line).</returns>
    public int? ApplyBusinessBillingModelFallback(int? productLineId, int? billingModelId)
    {
        if (!productLineId.HasValue || productLineId.Value <= 0)
        {
            _logger.LogDebug("ApplyBusinessBillingModelFallback: product line ID is null or invalid");
            return billingModelId;
        }

        // TODO: REPLACE WITH ACTUAL config lookup
        // In production, query fn_app_config_select_key_values('BUSINESS_PRODUCT_LINE', 'GENERAL')
        // For now, use hardcoded business product line IDs
        var knownBusinessProductLines = new[] { 100, 200, 300, 400 };

        if (!knownBusinessProductLines.Contains(productLineId.Value))
        {
            _logger.LogDebug(
                "ApplyBusinessBillingModelFallback: product line {Id} is not a business line",
                productLineId);
            return billingModelId;
        }

        _logger.LogDebug(
            "ApplyBusinessBillingModelFallback: product line {Id} is a business line, checking billing model",
            productLineId);

        // If billing model is null or invalid, apply business default
        if (!billingModelId.HasValue || billingModelId.Value <= 0)
        {
            // TODO: REPLACE WITH ACTUAL business default lookup
            // In production, query DEFAULT_BUSINESS_BILLING_MODEL table/config
            // For now, use a fixed default: 110 (annual)
            var businessDefault = 110;

            _logger.LogInformation(
                "ApplyBusinessBillingModelFallback: applying business default billing model {Default} for product line {ProductLine}",
                businessDefault, productLineId);

            return businessDefault;
        }

        return billingModelId;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.3: Load License Information
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.3: Load license information by keycode
    /// 
    /// Purpose: Given a license keycode, resolve it to license details
    /// including seats, category, expiration date, billing model, and all profile data
    /// needed for Sections 2.1-2.5 product determination.
    /// 
    /// Database Query (Section 1.5):
    ///   SELECT * FROM license WHERE keycode = @keycode
    ///   JOIN license_category ON license.license_category_id = license_category.license_category_id
    ///   (Equivalent to fn_license_select_license_profile)
    /// 
    /// Returns: Prepared license context model with all fields, or null if keycode is empty/not found
    /// 
    /// CRITICAL FIELDS for downstream:
    /// - CategoryTypeName: 'trial' or 'full' (Section 2.1 date determination)
    /// - AutorenewCycle: Years for renewal (Section 2.2 WIFI upgrade detection)
    /// - RetentionModelId: For Section 2.2.2 retention upgrade rules
    /// </summary>
    public async Task<CartOrderLicenseContext?> LoadLicenseByKeycodeAsync(
        string? keycode,
        CancellationToken ct = default)
    {
        // Early return: null/empty keycode
        if (string.IsNullOrWhiteSpace(keycode))
        {
            _logger.LogDebug("LoadLicenseByKeycodeAsync called with null/empty keycode");
            return null;
        }

        _logger.LogDebug("Loading license by keycode: {Keycode}", keycode);

        try
        {
            // Query repository: Get full license profile (Section 1.5 fn_license_select_license_profile)
            var licenseEntity = await _repository.GetLicenseByKeycodeAsync(keycode, ct);

            if (licenseEntity is null)
            {
                _logger.LogWarning("License not found for keycode: {Keycode}", keycode);
                return null;
            }

            // Load next process date for monthly billing conversions (Section 1.3.3)
            LicenseMessageEntity? messageEntity = null;
            if (licenseEntity.LicenseId > 0)
            {
                messageEntity = await _repository.GetLicenseMessageByIdAsync(licenseEntity.LicenseId, ct);
            }

            // Prepare license context from entity
            var context = new CartOrderLicenseContext
            {
                LicenseId = licenseEntity.LicenseId,
                Keycode = keycode,
                LicenseStatus = licenseEntity.LicenseStatus,
                LicenseCategory = licenseEntity.LicenseCategory?.LicenseCategoryName,
                LicenseCategoryName = licenseEntity.LicenseCategory?.LicenseCategoryName,
                LicenseSeats = licenseEntity.LicenseSeats,
                ExpirationDate = licenseEntity.ExpirationDate,
                CategoryTypeName = licenseEntity.CategoryTypeName,  // CRITICAL: 'trial' or 'full'
                LicenseAttributeLicenseValue = null,  // Will be populated from bundle JSON
                AutorenewCycle = licenseEntity.AutorenewCycle,  // CRITICAL: For Section 2.2 WIFI detection
                RetentionModelId = licenseEntity.RetentionModelId,
                RetentionTerm = licenseEntity.RetentionTerm,
                UsagePricingModelId = licenseEntity.UsagePricingModelId,
                StorageGb = licenseEntity.StorageGb,
                ProductPlatformId = licenseEntity.ProductPlatformId,
                LicenseKeycodeTypeId = licenseEntity.LicenseKeycodeTypeId,
                LicenseDistributionMethodId = licenseEntity.LicenseDistributionMethodId,
                LoadedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "License loaded: Keycode={Keycode}, LicenseId={LicenseId}, Category={Category}, " +
                "Type={Type}, Autorenew={Autorenew}, RetentionModel={RetentionModel}",
                keycode, licenseEntity.LicenseId, context.LicenseCategoryName,
                context.CategoryTypeName, context.AutorenewCycle, context.RetentionModelId);

            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading license by keycode: {Keycode}", keycode);
            throw;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.4: Load Existing Items
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.4: Extract and organize existing cart items
    /// 
    /// Purpose: Load line items from an existing cart, preparing them for
    /// updates or display. Includes validation that items exist and are well-formed.
    /// 
    /// Database Query (via cart loaded in Section 1.1):
    ///   SELECT * FROM cart_order_item WHERE cart_order_id = @order_id
    ///   SELECT * FROM cart_order_item_license WHERE cart_order_item_id = @item_id
    /// 
    /// Returns: Organized collection of prepared item contexts
    /// </summary>
    public async Task<List<CartOrderItemContext>> LoadExistingItemsAsync(
        Models.Responses.CartOrderResponse? cart,
        CancellationToken ct = default)
    {
        var items = new List<CartOrderItemContext>();

        // Early return: null/empty cart
        if (cart?.Items is null || cart.Items.Count == 0)
        {
            _logger.LogDebug("No items to load (cart is null or empty)");
            return items;
        }

        _logger.LogDebug("Loading {ItemCount} existing items from cart", cart.Items.Count);

        // Iterate through cart items and prepare context for each
        foreach (var item in cart.Items)
        {
            var context = new CartOrderItemContext
            {
                CartOrderItemId  = item.CartOrderItemId,
                CartOrderId      = item.CartOrderId,
                LineItem         = item.LineItem,
                ProductId        = item.ProductId,
                LicenseCategoryName              = item.LicenseCategoryName,
                Quantity                         = item.Quantity,
                LicenseSeats                     = item.Seats,
                StorageGb                        = item.StorageGb,
                Years                            = item.Years,
                StartDate                        = item.StartDate,
                ExpirationDate                   = item.ExpirationDate,
                // Populate billing model directly from the response row so that
                // Section 1.11 attribute-from-primary fallback and Section 2.1 billing
                // comparisons have the correct per-item value without extra DB calls.
                LicenseAttributeId              = item.LicenseAttributeId,
                LicenseAttributeLicenseValue     = item.LicenseAttributeLicenseValue,
                Keycode                          = item.Keycode,
                ItemHierarchyId                  = item.ItemHierarchyId,
                CartItemBundleId                 = item.CartItemBundleId,
                RetentionModelId                 = item.RetentionModelId  is { } rm  ? (byte)rm  : null,
                RetentionTerm                    = item.RetentionTerm     is { } rt  ? (byte)rt  : null,
                UsagePricingModelId              = item.UsagePricingModelId is { } up ? (byte)up : null,
                ProductPlatformId                = item.ProductPlatformId  is { } pp ? (byte)pp : null,
                LoadedAt                         = DateTime.UtcNow
            };

            items.Add(context);

            _logger.LogDebug(
                "Item loaded: LineItem={Line}, Product={ProductId}, Quantity={Qty}, Years={Years}, " +
                "Hierarchy={Hierarchy}, BillingModel={BillingModel}",
                context.LineItem, context.ProductId, context.Quantity, context.Years,
                context.ItemHierarchyId, context.LicenseAttributeLicenseValue);
        }

        _logger.LogInformation("Loaded {ItemCount} items from existing cart", items.Count);

        return items;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.2.1: Load Locale Mapping
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.2.1: Map locale to language and location codes
    /// 
    /// Purpose: Translate @locale string (e.g., 'en_US', 'de_DE', 'ja_JP') 
    /// to language_code and location_code pair needed for regional lookups.
    /// 
    /// Database Query:
    ///   SELECT language_code, location_code FROM locale_language_location 
    ///   WHERE locale = @locale
    ///   OR CALL fn_locale_to_lang_loc(@locale, @language_code OUT, @location_code OUT)
    /// 
    /// Returns: Tuple (languageCode, locationCode), or defaults ("en", "US") if not found
    /// </summary>
    public async Task<(string LanguageCode, string LocationCode)> LoadLocaleCodesByLocaleAsync(
        string? locale,
        CancellationToken ct = default)
    {
        // Default: English, United States
        const string defaultLanguage = "en";
        const string defaultLocation = "US";

        // Early return: null/empty locale
        if (string.IsNullOrWhiteSpace(locale))
        {
            _logger.LogDebug("Locale is null or empty, using defaults: {Language}/{Location}", 
                defaultLanguage, defaultLocation);
            return (defaultLanguage, defaultLocation);
        }

        _logger.LogDebug("Loading locale codes for locale: {Locale}", locale);

        try
        {
            // Query repository for locale mapping
            var localeEntity = await _repository.GetLocaleByCodeAsync(locale, ct);

            if (localeEntity is null)
            {
                _logger.LogWarning("Locale mapping not found: {Locale}, using defaults", locale);
                return (defaultLanguage, defaultLocation);
            }

            var languageCode = localeEntity.LanguageCode ?? defaultLanguage;
            var locationCode = localeEntity.LocationCode ?? defaultLocation;

            _logger.LogDebug(
                "Locale codes loaded: Locale={Locale}, Language={Language}, Location={Location}",
                locale, languageCode, locationCode);

            return (languageCode, locationCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading locale codes for {Locale}, using defaults", locale);
            return (defaultLanguage, defaultLocation);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.9 & 1.9.1: Load Product Line by License Category
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.9 & 1.9.1: Get product line for license category and locale
    /// 
    /// Purpose: Map license_category_id + locale to product_line_id for upgrade determination.
    /// Used to identify which products are available for upgrades in a specific region/category.
    /// 
    /// Database Query:
    ///   SELECT product_line_id FROM license_category_product_line
    ///   WHERE license_category_id = @licenseCategoryId
    ///   AND language_code = @languageCode
    ///   AND location_code = @locationCode
    /// 
    /// Returns: product_line_id if found, null if not found (fallback to license.product_line_id)
    /// </summary>
    public async Task<int?> LoadProductLineByLicenseCategoryAsync(
        int? licenseCategoryId,
        string? languageCode,
        string? locationCode,
        CancellationToken ct = default)
    {
        // Early return: null category
        if (!licenseCategoryId.HasValue || licenseCategoryId.Value <= 0)
        {
            _logger.LogDebug("LicenseCategoryId is null or invalid, skipping product line lookup");
            return null;
        }

        _logger.LogDebug(
            "Loading product line for category: LicenseCategoryId={CategoryId}, Language={Language}, Location={Location}",
            licenseCategoryId, languageCode, locationCode);

        try
        {
            // Query repository for product line mapping
            var productLineEntity = await _repository.GetProductLineByLicenseCategoryAndLocaleAsync(
                licenseCategoryId.Value,
                languageCode,
                locationCode,
                ct);

            if (productLineEntity is null)
            {
                _logger.LogDebug(
                    "Product line not found for category {CategoryId}, using fallback",
                    licenseCategoryId);
                return null;
            }

            _logger.LogDebug(
                "Product line loaded: Category={CategoryId}, ProductLineId={ProductLineId}",
                licenseCategoryId, productLineEntity.ProductLineId);

            return productLineEntity.ProductLineId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product line for category {CategoryId}", 
                licenseCategoryId);
            throw;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.5: Prepare Product Information
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.5: Load product information for items in the request or cart
    /// 
    /// Purpose: Given a collection of product IDs, fetch complete product entities
    /// from the database and prepare them for pricing and display operations.
    /// 
    /// Database Query (Optimized):
    ///   SELECT * FROM product WHERE product_id IN (@ids)
    ///   INCLUDE license_category  (single query via .Include(), not N+1 queries)
    /// 
    /// Returns: Dictionary mapping product ID → prepared product context
    /// </summary>
    public async Task<Dictionary<int, CartOrderProductContext>> PrepareProductsAsync(
        IEnumerable<int>? productIds,
        CancellationToken ct = default)
    {
        var products = new Dictionary<int, CartOrderProductContext>();

        // Early return: null/empty product list
        if (productIds is null)
        {
            _logger.LogDebug("PrepareProductsAsync called with null productIds");
            return products;
        }

        var idList = productIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            _logger.LogDebug("PrepareProductsAsync called with empty productIds");
            return products;
        }

        _logger.LogDebug("Preparing {ProductCount} products", idList.Count);

        try
        {
            // ✅ OPTIMIZED: Batch-load all products with their categories in a single query
            // Avoids N+1 query problem: 1 query instead of 2N queries
            var productEntities = await _repository.GetProductsByIdBatchAsync(idList, ct);

            if (productEntities.Count == 0)
            {
                _logger.LogWarning("No products found for {ProductIdCount} requested IDs", idList.Count);
                return products;
            }

            // Prepare context for each product
            foreach (var productEntity in productEntities.Values)
            {
                var context = new CartOrderProductContext
                {
                    ProductId = productEntity.ProductId,
                    Description = productEntity.ProductDescription,
                    ProductTypeId = productEntity.ProductTypeId,
                    ProductFamilyId = productEntity.ProductFamilyId,
                    ProductLineId = productEntity.ProductLineId,
                    ProductLifecycleId = productEntity.ProductLifecycleId,
                    LicenseKeycodeTypeId = productEntity.LicenseKeycodeTypeId,
                    LicenseCategoryId = productEntity.LicenseCategoryId,
                    // ✅ OPTIMIZED: LicenseCategory already loaded by .Include()
                    LicenseCategoryName = productEntity.LicenseCategory?.LicenseCategoryName,
                    LicenseCategoryDescription = productEntity.LicenseCategory?.LicenseCategoryDescription,
                    LoadedAt = DateTime.UtcNow
                };

                products[productEntity.ProductId] = context;

                _logger.LogDebug(
                    "Product prepared: ProductId={Id}, Category={Category}, Type={Type}",
                    context.ProductId, context.LicenseCategoryName ?? "NONE", 
                    context.ProductTypeId);
            }

            _logger.LogInformation("Prepared {PreparedCount} of {RequestedCount} products",
                products.Count, idList.Count);

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error batch preparing {ProductCount} products", idList.Count);
            throw;
        }
    }

    /// <summary>
    /// SECTION 1.5B: Prepare products from cart items
    /// 
    /// Convenience method that extracts product IDs from items and calls PrepareProductsAsync.
    /// </summary>
    public async Task<Dictionary<int, CartOrderProductContext>> PrepareProductsFromItemsAsync(
        List<CartOrderItemContext>? items,
        CancellationToken ct = default)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("No items provided for product preparation");
            return new Dictionary<int, CartOrderProductContext>();
        }

        var productIds = items.Select(i => i.ProductId).Distinct();
        return await PrepareProductsAsync(productIds, ct);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.4.1.1: OTSF Retention Model Validation
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.4.1.1: Validate that no OTSF item carries <c>retention_model_id = 7</c>.
    ///
    /// SQL equivalent:
    /// <code>
    /// IF EXISTS (SELECT * FROM @item_table i
    ///            WHERE i.license_category_name = 'OTSF' AND i.retention_model_id = 7)
    ///   SELECT @response_code = -1, @message = '...' RETURN
    /// </code>
    ///
    /// When the condition is violated the method throws <see cref="InvalidOperationException"/>
    /// so that the calling service layer can map it to the appropriate HTTP 422 / error response.
    /// The check is category-name based to mirror the SQL exactly; 'CBSB' is also
    /// treated as OTSF per the 2022-01-06 remark in the procedure header.
    /// </summary>
    /// <param name="items">Items produced by <see cref="ParseItemJsonAsync"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when an OTSF (or CBSB) item requests <c>retention_model_id = 7</c>.
    /// </exception>
    public void ValidateOtsfRetentionModel(List<CartOrderItemContext> items)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("ValidateOtsfRetentionModel: no items to validate");
            return;
        }

        // SQL: license_category_name = 'OTSF' AND retention_model_id = 7
        // 'CBSB' is also covered per 2022-01-06 history note.
        var violatingItem = items.FirstOrDefault(i =>
            i.RetentionModelId == 7 &&
            (string.Equals(i.LicenseCategoryName, "OTSF", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(i.LicenseCategoryName, "CBSB", StringComparison.OrdinalIgnoreCase)));

        if (violatingItem is not null)
        {
            _logger.LogWarning(
                "ValidateOtsfRetentionModel: OTSF/CBSB item LineItem={Line} " +
                "has retention_model_id=7 which is not permitted for partner cart",
                violatingItem.LineItem);

            // Mirrors: SELECT @response_code = -1, @message = '...' RETURN
            throw new InvalidOperationException(
                "No product unit price found in partner_pricing_tier");
        }

        _logger.LogDebug("ValidateOtsfRetentionModel: all {Count} items passed", items.Count);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.5.1: SFDC Unit Override
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.5.1: Populate the SFDC <em>unit override</em> table (in memory).
    ///
    /// SQL equivalent:
    /// <code>
    /// IF @site_id = 'SFDC'
    ///   INSERT INTO @unit_override (cart_order_id, item_id, unit_price, usage_price,
    ///                               item_total, license_seats, license_category_name)
    ///   SELECT @cart_order_id, i.item_id,
    ///          i.unit_price, i.usage_price, i.item_total,
    ///          i.license_seats - l.license_seats,   -- delta seats
    ///          i.license_category_name
    ///   FROM @item_table i
    ///   LEFT JOIN @license_table l ON l.license_category_name = i.license_category_name
    ///   WHERE i.unit_price IS NOT NULL
    /// </code>
    ///
    /// The delta (<c>item.LicenseSeats - license.LicenseSeats</c>) represents the number of
    /// <em>new</em> seats being added on top of what the customer already owns.
    /// Results are returned as a list of <see cref="SfdcUnitOverride"/> records; callers
    /// may apply these to pricing downstream.
    /// </summary>
    /// <param name="siteId">The order site ID (e.g. <c>"SFDC"</c>).</param>
    /// <param name="items">New items from <see cref="ParseItemJsonAsync"/>.</param>
    /// <param name="license">License context loaded in Section 1.3 (may be null).</param>
    /// <returns>
    /// Populated override list when <paramref name="siteId"/> is <c>"SFDC"</c>;
    /// empty list otherwise.
    /// </returns>
    public List<SfdcUnitOverride> BuildSfdcUnitOverrides(
        string? siteId,
        List<CartOrderItemContext> items,
        CartOrderLicenseContext? license)
    {
        var result = new List<SfdcUnitOverride>();

        // SQL: IF @site_id = 'SFDC'
        if (!string.Equals(siteId, "SFDC", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("BuildSfdcUnitOverrides: site_id={SiteId} – not SFDC, skipping", siteId);
            return result;
        }

        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("BuildSfdcUnitOverrides: no items to process");
            return result;
        }

        _logger.LogDebug("BuildSfdcUnitOverrides: building overrides for {Count} SFDC items", items.Count);

        foreach (var item in items)
        {
            // SQL: WHERE i.unit_price IS NOT NULL
            // (UnitPrice is not on CartOrderItemContext for new items; the field comes from
            //  @item_json which carries unit_price.  We skip items without an override price.)
            // In the current model we don't carry UnitPrice on CartOrderItemContext for new items
            // so we emit the record for every item with a license seat delta – callers decide
            // whether to apply pricing.

            // SQL: i.license_seats - l.license_seats
            // LEFT JOIN means l.license_seats may be null → delta = item seats
            int licenseSeatsDelta = item.LicenseSeats.HasValue
                ? item.LicenseSeats.Value -
                  (license?.LicenseSeats ?? 0)
                : 0;

            var entry = new SfdcUnitOverride
            {
                LineItem             = item.LineItem,
                LicenseCategoryName  = item.LicenseCategoryName,
                DeltaLicenseSeats    = licenseSeatsDelta,
            };

            result.Add(entry);

            _logger.LogDebug(
                "BuildSfdcUnitOverrides: LineItem={Line} Category={Category} DeltaSeats={Delta}",
                entry.LineItem, entry.LicenseCategoryName, entry.DeltaLicenseSeats);
        }

        _logger.LogInformation(
            "BuildSfdcUnitOverrides: produced {Count} unit-override entries for SFDC order",
            result.Count);

        return result;
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.6: Upgrade Seat Calculation  (SQL: 1.6 upgrade license_seats)
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.6: Calculate upgrade <c>license_seats</c> for items where <c>years = 0</c>.
    ///
    /// SQL equivalent:
    /// <code>
    /// UPDATE @item_table
    /// SET license_seats = ISNULL(uo.license_seats, i.license_seats - l.license_seats)
    /// FROM @item_table i
    /// INNER JOIN @license_table l  ON l.license_category_name = i.license_category_name
    /// LEFT JOIN  @unit_override uo ON uo.license_category_name = i.license_category_name
    /// WHERE i.years = 0 AND i.amended_contract IS NULL
    /// </code>
    ///
    /// Logic in plain English:
    /// <list type="number">
    ///   <item>Only touch items where <c>Years == 0</c> AND no amended-contract flag.</item>
    ///   <item>If an SFDC unit-override entry exists for the item's category → use its
    ///         <c>DeltaLicenseSeats</c> value.</item>
    ///   <item>Otherwise → delta = <c>item.LicenseSeats - license.LicenseSeats</c>.</item>
    /// </list>
    ///
    /// Items are mutated in-place.
    /// </summary>
    /// <param name="items">Item list to update.</param>
    /// <param name="license">License context for seat baseline (may be null).</param>
    /// <param name="sfdcOverrides">Unit-override entries from <see cref="BuildSfdcUnitOverrides"/>.</param>
    public void CalculateUpgradeLicenseSeats(
        List<CartOrderItemContext> items,
        CartOrderLicenseContext? license,
        IReadOnlyList<SfdcUnitOverride>? sfdcOverrides = null)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("CalculateUpgradeLicenseSeats: no items to process");
            return;
        }

        // Build a fast lookup: license_category_name → delta seats from SFDC overrides
        var overrideLookup = sfdcOverrides?
            .Where(o => o.LicenseCategoryName is not null)
            .GroupBy(o => o.LicenseCategoryName!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().DeltaLicenseSeats, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        int updated = 0;

        foreach (var item in items)
        {
            // SQL: WHERE i.years = 0 AND i.amended_contract IS NULL
            // amended_contract is not yet tracked on CartOrderItemContext – treated as null.
            if (item.Years != 0)
                continue;

            // SQL: INNER JOIN @license_table l ON l.license_category_name = i.license_category_name
            // If no matching license row → skip (mirrors INNER JOIN semantics)
            if (license is null ||
                !string.Equals(item.LicenseCategoryName, license.LicenseCategoryName,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            int newSeats;

            // SQL: ISNULL(uo.license_seats, i.license_seats - l.license_seats)
            if (overrideLookup.TryGetValue(item.LicenseCategoryName ?? string.Empty, out int overrideSeats))
            {
                newSeats = overrideSeats;
            }
            else
            {
                newSeats = (item.LicenseSeats ?? 0) - (license.LicenseSeats ?? 0);
            }

            _logger.LogDebug(
                "CalculateUpgradeLicenseSeats: LineItem={Line} Category={Category} " +
                "OldSeats={Old} → NewSeats={New}",
                item.LineItem, item.LicenseCategoryName, item.LicenseSeats, newSeats);

            item.LicenseSeats = newSeats;
            updated++;
        }

        _logger.LogInformation(
            "CalculateUpgradeLicenseSeats: updated {Count} of {Total} items",
            updated, items.Count);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.7 & 1.7.1: Monthly-to-Annual Conversion
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTIONS 1.7 &amp; 1.7.1: Apply monthly-to-annual conversion to primary and secondary items.
    ///
    /// SQL equivalents:
    /// <code>
    /// -- 1.7) primary
    /// UPDATE @item_table
    /// SET start_date     = @next_process_date,
    ///     product_type_id = 2
    /// FROM @item_table i
    /// INNER JOIN @license_table l ON l.license_category_name = i.license_category_name
    /// WHERE l.license_attribute_license_value IN (12,110,111,112,210,211,212,13,113,213)
    ///   AND @license_attribute_license_value  IN (20,120,220)
    ///
    /// -- 1.7.1) secondary (i2)
    /// UPDATE i2
    /// SET start_date     = @next_process_date,
    ///     product_type_id = 2
    /// FROM @item_table i
    /// INNER JOIN @license_table l ON l.license_category_name = i.license_category_name
    /// INNER JOIN @item_table i2
    ///     ON  i2.cart_item_bundle_id = i.cart_item_bundle_id
    ///     AND i2.item_hierarchy_id   = 2
    /// WHERE l.license_attribute_license_value IN (12,110,111,112,210,211,212,13,113,213)
    ///   AND @license_attribute_license_value  IN (20,120,220)
    /// </code>
    ///
    /// The monthly billing model codes (12, 110–213 series) represent the <em>existing</em>
    /// license's billing model.  The bundle-level codes (20, 120, 220) are the <em>requested</em>
    /// billing model from the bundle JSON.  When the existing license is monthly and the new
    /// order selects an annual product, the start date is deferred to the next billing cycle.
    ///
    /// Only <see cref="CartOrderItemContext.StartDate"/> and
    /// <see cref="CartOrderItemContext.ProductTypeId"/> are mutated; no DB writes occur.
    /// </summary>
    /// <param name="items">All items (primary and secondary).</param>
    /// <param name="license">License profile loaded in Section 1.3.</param>
    /// <param name="bundleBillingModelId">
    ///   <c>@license_attribute_license_value</c> from the bundle JSON
    ///   (i.e. <see cref="BundleContext.LicenseAttributeLicenseValue"/>).
    /// </param>
    /// <param name="nextProcessDate">
    ///   <c>@next_process_date</c> from <c>license_message</c> (Section 1.3.3).
    /// </param>
    public void ApplyMonthlyToAnnualConversion(
        List<CartOrderItemContext> items,
        CartOrderLicenseContext? license,
        int? bundleBillingModelId,
        DateTime? nextProcessDate)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("ApplyMonthlyToAnnualConversion: no items to process");
            return;
        }

        // SQL: WHERE l.license_attribute_license_value IN (12,110,111,112,210,211,212,13,113,213)
        //      AND @license_attribute_license_value IN (20,120,220)
        //
        // Both conditions must hold.  If either is false the entire UPDATE is a no-op.

        static bool IsMonthlyLicenseBillingModel(int? val) =>
            val is 12 or 110 or 111 or 112 or 210 or 211 or 212 or 13 or 113 or 213;

        static bool IsAnnualBundleBillingModel(int? val) =>
            val is 20 or 120 or 220;

        if (!IsAnnualBundleBillingModel(bundleBillingModelId))
        {
            _logger.LogDebug(
                "ApplyMonthlyToAnnualConversion: bundle billing model {Model} is not an annual " +
                "conversion target (20/120/220) – skipping",
                bundleBillingModelId);
            return;
        }

        if (!IsMonthlyLicenseBillingModel(license?.LicenseAttributeLicenseValue))
        {
            _logger.LogDebug(
                "ApplyMonthlyToAnnualConversion: license billing model {Model} is not a monthly " +
                "source (12/110-213 series) – skipping",
                license?.LicenseAttributeLicenseValue);
            return;
        }

        if (nextProcessDate is null)
        {
            _logger.LogWarning(
                "ApplyMonthlyToAnnualConversion: conditions met but next_process_date is null – " +
                "start_date will not be updated");
        }

        _logger.LogDebug(
            "ApplyMonthlyToAnnualConversion: conditions met – " +
            "LicenseBillingModel={LicenseModel}, BundleModel={BundleModel}, " +
            "NextProcessDate={NextDate}",
            license!.LicenseAttributeLicenseValue, bundleBillingModelId, nextProcessDate);

        // ── 1.7) Primary items ────────────────────────────────────────────────────
        // SQL INNER JOIN @license_table l ON l.license_category_name = i.license_category_name
        // means only items whose category matches the license row are updated.
        int primaryUpdated = 0;
        int secondaryUpdated = 0;

        // Collect bundle IDs of primary items that qualify (used for 1.7.1 secondary pass).
        var qualifyingBundleIds = new HashSet<int>();

        foreach (var item in items)
        {
            if (!string.Equals(item.LicenseCategoryName, license.LicenseCategoryName,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            // SQL: SET start_date = @next_process_date, product_type_id = 2
            item.StartDate    = nextProcessDate;
            item.ProductTypeId = 2;
            primaryUpdated++;

            if (item.CartItemBundleId.HasValue)
                qualifyingBundleIds.Add(item.CartItemBundleId.Value);

            _logger.LogDebug(
                "ApplyMonthlyToAnnualConversion [1.7]: LineItem={Line} Category={Category} " +
                "→ StartDate={Date}, ProductTypeId=2",
                item.LineItem, item.LicenseCategoryName, nextProcessDate);
        }

        // ── 1.7.1) Secondary items ────────────────────────────────────────────────
        // SQL: INNER JOIN @item_table i2
        //        ON  i2.cart_item_bundle_id = i.cart_item_bundle_id
        //        AND i2.item_hierarchy_id   = 2
        foreach (var item in items)
        {
            if (item.ItemHierarchyId != 2)
                continue;

            if (!item.CartItemBundleId.HasValue ||
                !qualifyingBundleIds.Contains(item.CartItemBundleId.Value))
                continue;

            item.StartDate     = nextProcessDate;
            item.ProductTypeId = 2;
            secondaryUpdated++;

            _logger.LogDebug(
                "ApplyMonthlyToAnnualConversion [1.7.1]: LineItem={Line} BundleId={Bundle} " +
                "→ StartDate={Date}, ProductTypeId=2",
                item.LineItem, item.CartItemBundleId, nextProcessDate);
        }

        _logger.LogInformation(
            "ApplyMonthlyToAnnualConversion: updated {Primary} primary, {Secondary} secondary items",
            primaryUpdated, secondaryUpdated);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.11: Item Enrichment (Billing Model, Usage/Retention/Platform Defaults)
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.11.1: Apply billing model fallback from primary item to secondary items.
    ///
    /// SQL equivalent:
    /// <code>
    /// UPDATE secondary_items
    /// SET license_attribute_license_value = primary_item.license_attribute_license_value
    /// WHERE secondary_items.license_attribute_license_value IS NULL
    ///   AND secondary_items.item_hierarchy_id = 2
    /// </code>
    ///
    /// Logic: Secondary items (ItemHierarchyId=2) inherit the billing model from their
    /// linked primary item (via CartItemBundleId).  No-op if billing model is already set.
    /// </summary>
    /// <param name="items">Items to enrich in-place.</param>
    /// <param name="ct">Cancellation token.</param>
    public void EnrichBillingModelFallback(List<CartOrderItemContext> items, CancellationToken ct = default)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("EnrichBillingModelFallback: no items to enrich");
            return;
        }

        // Build a map: cart_item_bundle_id → primary_item.license_attribute_license_value
        var primaryBillingByBundle = items
            .Where(i => i.ItemHierarchyId == 1 && i.CartItemBundleId.HasValue && i.LicenseAttributeLicenseValue.HasValue)
            .GroupBy(i => i.CartItemBundleId!.Value)
            .ToDictionary(g => g.Key, g => g.First().LicenseAttributeLicenseValue!.Value);

        int updated = 0;

        foreach (var item in items)
        {
            // Only apply to secondary items (ItemHierarchyId=2) that lack a billing model
            if (item.ItemHierarchyId != 2 || item.LicenseAttributeLicenseValue.HasValue)
                continue;

            if (!item.CartItemBundleId.HasValue ||
                !primaryBillingByBundle.TryGetValue(item.CartItemBundleId.Value, out var primaryBilling))
            {
                _logger.LogDebug(
                    "EnrichBillingModelFallback: LineItem={Line} secondary item has no linked primary billing model",
                    item.LineItem);
                continue;
            }

            item.LicenseAttributeLicenseValue = primaryBilling;
            updated++;

            _logger.LogDebug(
                "EnrichBillingModelFallback: LineItem={Line} secondary item → BillingModel={Billing}",
                item.LineItem, primaryBilling);
        }

        _logger.LogInformation(
            "EnrichBillingModelFallback: updated {Count} secondary items",
            updated);
    }

    /// <summary>
    /// SECTION 1.11.2: Enrich usage pricing model with fallback chain:
    /// 1. Use item's existing value (from JSON or loaded)
    /// 2. Fall back to license's value
    /// 3. Apply OTSF default (e.g., 1) if category is OTSF
    /// 4. Apply CBEP default (e.g., 2) if category is CBEP
    ///
    /// SQL equivalent (simplified):
    /// <code>
    /// UPDATE items
    /// SET usage_pricing_model_id = ISNULL(item.usage_pricing_model_id,
    ///     ISNULL(license.usage_pricing_model_id,
    ///     CASE WHEN item.license_category_name = 'OTSF' THEN 1 ELSE NULL END))
    /// </code>
    ///
    /// Enriches items in-place. Non-fatal: items without a resolved value retain null.
    /// </summary>
    /// <param name="items">Items to enrich in-place.</param>
    /// <param name="license">License context for fallback values.</param>
    /// <param name="otsfDefaultId">Default usage pricing model ID for OTSF items (typically 1).</param>
    /// <param name="cbepDefaultId">Default usage pricing model ID for CBEP items (typically 2).</param>
    public void EnrichUsagePricingModel(
        List<CartOrderItemContext> items,
        CartOrderLicenseContext? license,
        byte otsfDefaultId = 1,
        byte cbepDefaultId = 2)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("EnrichUsagePricingModel: no items to enrich");
            return;
        }

        int enriched = 0;

        foreach (var item in items)
        {
            // Skip if already has a value
            if (item.UsagePricingModelId.HasValue)
                continue;

            byte? resolvedValue = null;

            // Step 2: License fallback
            if (license?.UsagePricingModelId.HasValue == true)
            {
                resolvedValue = license.UsagePricingModelId;
                _logger.LogDebug(
                    "EnrichUsagePricingModel: LineItem={Line} → {Value} (from license)",
                    item.LineItem, resolvedValue);
            }
            // Step 3-4: Category defaults
            else if (!string.IsNullOrWhiteSpace(item.LicenseCategoryName))
            {
                if (string.Equals(item.LicenseCategoryName, "OTSF", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedValue = otsfDefaultId;
                    _logger.LogDebug(
                        "EnrichUsagePricingModel: LineItem={Line} OTSF → {Value} (OTSF default)",
                        item.LineItem, resolvedValue);
                }
                else if (string.Equals(item.LicenseCategoryName, "CBEP", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedValue = cbepDefaultId;
                    _logger.LogDebug(
                        "EnrichUsagePricingModel: LineItem={Line} CBEP → {Value} (CBEP default)",
                        item.LineItem, resolvedValue);
                }
            }

            if (resolvedValue.HasValue)
            {
                item.UsagePricingModelId = resolvedValue;
                enriched++;
            }
        }

        _logger.LogInformation(
            "EnrichUsagePricingModel: enriched {Count} of {Total} items",
            enriched, items.Count);
    }

    /// <summary>
    /// SECTION 1.11.3: Enrich retention model with fallback chain:
    /// 1. Use item's existing value (from JSON or loaded)
    /// 2. Fall back to license's value
    /// 3. Apply OTSF/CBSB default (e.g., 5) if category is OTSF or CBSB
    ///
    /// SQL equivalent (simplified):
    /// <code>
    /// UPDATE items
    /// SET retention_model_id = ISNULL(item.retention_model_id,
    ///     ISNULL(license.retention_model_id,
    ///     CASE WHEN item.license_category_name IN ('OTSF', 'CBSB') THEN 5 ELSE NULL END))
    /// </code>
    ///
    /// Enriches items in-place. Non-fatal: items without a resolved value retain null.
    /// </summary>
    /// <param name="items">Items to enrich in-place.</param>
    /// <param name="license">License context for fallback values.</param>
    /// <param name="otsfCbsbDefaultId">Default retention model ID for OTSF/CBSB items (typically 5).</param>
    public void EnrichRetentionModel(
        List<CartOrderItemContext> items,
        CartOrderLicenseContext? license,
        byte otsfCbsbDefaultId = 5)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("EnrichRetentionModel: no items to enrich");
            return;
        }

        int enriched = 0;

        foreach (var item in items)
        {
            // Skip if already has a value
            if (item.RetentionModelId.HasValue)
                continue;

            byte? resolvedValue = null;

            // Step 2: License fallback
            if (license?.RetentionModelId.HasValue == true)
            {
                resolvedValue = license.RetentionModelId;
                _logger.LogDebug(
                    "EnrichRetentionModel: LineItem={Line} → {Value} (from license)",
                    item.LineItem, resolvedValue);
            }
            // Step 3: Category defaults (OTSF/CBSB)
            else if (!string.IsNullOrWhiteSpace(item.LicenseCategoryName) &&
                (string.Equals(item.LicenseCategoryName, "OTSF", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(item.LicenseCategoryName, "CBSB", StringComparison.OrdinalIgnoreCase)))
            {
                resolvedValue = otsfCbsbDefaultId;
                _logger.LogDebug(
                    "EnrichRetentionModel: LineItem={Line} {Category} → {Value} (OTSF/CBSB default)",
                    item.LineItem, item.LicenseCategoryName, resolvedValue);
            }

            if (resolvedValue.HasValue)
            {
                item.RetentionModelId = resolvedValue;
                enriched++;
            }
        }

        _logger.LogInformation(
            "EnrichRetentionModel: enriched {Count} of {Total} items",
            enriched, items.Count);
    }

    /// <summary>
    /// SECTION 1.11.4: Enrich product platform with fallback chain:
    /// 1. Use item's existing value (from JSON or loaded)
    /// 2. Fall back to license's value
    /// 3. Apply CBEP default (e.g., 1) if category is CBEP
    ///
    /// SQL equivalent (simplified):
    /// <code>
    /// UPDATE items
    /// SET product_platform_id = ISNULL(item.product_platform_id,
    ///     ISNULL(license.product_platform_id,
    ///     CASE WHEN item.license_category_name = 'CBEP' THEN 1 ELSE NULL END))
    /// </code>
    ///
    /// Enriches items in-place. Non-fatal: items without a resolved value retain null.
    /// </summary>
    /// <param name="items">Items to enrich in-place.</param>
    /// <param name="license">License context for fallback values.</param>
    /// <param name="cbepDefaultId">Default product platform ID for CBEP items (typically 1).</param>
    public void EnrichProductPlatform(
        List<CartOrderItemContext> items,
        CartOrderLicenseContext? license,
        byte cbepDefaultId = 1)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("EnrichProductPlatform: no items to enrich");
            return;
        }

        int enriched = 0;

        foreach (var item in items)
        {
            // Skip if already has a value
            if (item.ProductPlatformId.HasValue)
                continue;

            byte? resolvedValue = null;

            // Step 2: License fallback
            if (license?.ProductPlatformId.HasValue == true)
            {
                resolvedValue = license.ProductPlatformId;
                _logger.LogDebug(
                    "EnrichProductPlatform: LineItem={Line} → {Value} (from license)",
                    item.LineItem, resolvedValue);
            }
            // Step 3: Category defaults (CBEP)
            else if (!string.IsNullOrWhiteSpace(item.LicenseCategoryName) &&
                string.Equals(item.LicenseCategoryName, "CBEP", StringComparison.OrdinalIgnoreCase))
            {
                resolvedValue = cbepDefaultId;
                _logger.LogDebug(
                    "EnrichProductPlatform: LineItem={Line} CBEP → {Value} (CBEP default)",
                    item.LineItem, resolvedValue);
            }

            if (resolvedValue.HasValue)
            {
                item.ProductPlatformId = resolvedValue;
                enriched++;
            }
        }

        _logger.LogInformation(
            "EnrichProductPlatform: enriched {Count} of {Total} items",
            enriched, items.Count);
    }

    /// <summary>
    /// SECTION 1.15: Apply default storage GB for items with null storage.
    ///
    /// SQL equivalent:
    /// <code>
    /// UPDATE items
    /// SET storage_gb = fn_get_item_storage_gb(
    ///     item.license_category_name,
    ///     product.product_id,
    ///     item.usage_pricing_model_id,
    ///     license.storage_gb)
    /// WHERE items.storage_gb IS NULL
    /// </code>
    ///
    /// Logic: Preserve SQL control flow by delegating storage resolution to repository.
    /// No storage calculation rules are implemented in the service layer.
    ///
    /// Enriches items in-place. Non-fatal: items that cannot be resolved retain null.
    /// </summary>
    /// <param name="items">Items to enrich in-place.</param>
    /// <param name="products">Product dictionary for lookups.</param>
    /// <param name="license">License context for fallback storage value.</param>
    public void EnrichDefaultStorageGb(
        List<CartOrderItemContext> items,
        Dictionary<int, CartOrderProductContext> products,
        CartOrderLicenseContext? license)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("EnrichDefaultStorageGb: no items to enrich");
            return;
        }

        // TODO: Replace Repository.GetItemStorageGbAsync with the actual SQL
        // fn_get_item_storage_gb implementation once the SQL function definition
        // and full database access are available.

        int enriched = 0;

        foreach (var item in items)
        {
            // SQL: WHERE storage_gb IS NULL
            if (item.StorageGb.HasValue)
                continue;

            var resolvedStorage = _repository.GetItemStorageGbAsync(
                item.Quantity,
                item.LicenseCategoryName ?? string.Empty,
                item.UsagePricingModelId,
                CancellationToken.None).GetAwaiter().GetResult();

            if (resolvedStorage.HasValue)
            {
                item.StorageGb = resolvedStorage;
                enriched++;

                _logger.LogDebug(
                    "EnrichDefaultStorageGb: LineItem={Line} resolved StorageGb={Storage}",
                    item.LineItem, resolvedStorage);
            }
        }

        _logger.LogInformation(
            "EnrichDefaultStorageGb: enriched {Count} of {Total} items",
            enriched, items.Count);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.9.1.1 & 1.9.1.2: Business Billing Model Resolution
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.9.1.1 & 1.9.1.2: Resolve business billing model with location-based override.
    ///
    /// SQL equivalent:
    /// <code>
    /// IF @product_line_id IN (SELECT [key] FROM fn_app_config_select_key_values('BUSINESS_PRODUCT_LINE', 'GENERAL'))
    /// BEGIN
    ///   IF EXISTS (SELECT 1 FROM license_category_product_line_license_attribute_license_value 
    ///              WHERE product_line_id = @product_line_id AND location_code = @location_code)
    ///   BEGIN
    ///     -- Location-specific billing model found: use it if current is null or default
    ///     IF @license_attribute_license_value IS NULL 
    ///        OR @license_attribute_license_value IN (SELECT ... FROM @DEFAULT_BUSINESS_BILLING_MODEL)
    ///       SELECT @license_attribute_license_value = ..., @license_attribute_id = ...
    ///   END
    ///   ELSE
    ///   BEGIN
    ///     -- No location-specific entry: use DEFAULT_BUSINESS_BILLING_MODEL
    ///     SELECT @license_attribute_license_value = ISNULL(@current, default_value), ...
    ///   END
    /// END
    /// </code>
    ///
    /// Non-fatal: Returns input if not a business product line or if lookup fails.
    /// </summary>
    /// <param name="productLineId">Product line ID (may be null or non-business).</param>
    /// <param name="locationCode">Location code for region-based billing model.</param>
    /// <param name="licenseCategoryIds">License categories for lookup context.</param>
    /// <param name="currentBillingModelId">Current global billing model (may be null).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Resolved (billingModelId, licenseAttributeId) tuple, or input if not applicable.</returns>
    public async Task<(int? BillingModelId, int? LicenseAttributeId)> ApplyLocationBasedBillingModelAsync(
        int? productLineId,
        string? locationCode,
        IEnumerable<int>? licenseCategoryIds,
        int? currentBillingModelId,
        int? currentLicenseAttributeId,
        CancellationToken ct = default)
    {
        // Early return: not a business product line
        if (!productLineId.HasValue || productLineId.Value <= 0)
        {
            _logger.LogDebug(
                "ApplyLocationBasedBillingModelAsync: product line {Id} not business; returning input",
                productLineId);
            return (currentBillingModelId, currentLicenseAttributeId);
        }

        // TODO: Check if product line is in BUSINESS_PRODUCT_LINE config
        // For now, assume 300 and 55 are business
        var businessProductLines = new[] { 300, 55 };
        if (!businessProductLines.Contains(productLineId.Value))
        {
            _logger.LogDebug(
                "ApplyLocationBasedBillingModelAsync: product line {Id} not in business set",
                productLineId);
            return (currentBillingModelId, currentLicenseAttributeId);
        }

        if (string.IsNullOrWhiteSpace(locationCode) || licenseCategoryIds is null)
        {
            _logger.LogDebug(
                "ApplyLocationBasedBillingModelAsync: invalid location or categories, using input");
            return (currentBillingModelId, currentLicenseAttributeId);
        }

        _logger.LogDebug(
            "ApplyLocationBasedBillingModelAsync: checking location-based billing for ProductLine={Line}, Location={Loc}",
            productLineId, locationCode);

        try
        {
            // Query repository for location-specific billing model
            var locationBilling = await _repository.GetLocationBasedBillingModelAsync(
                productLineId.Value,
                locationCode,
                licenseCategoryIds,
                ct);

            if (locationBilling.HasValue)
            {
                var (billingModelId, licenseAttributeId) = locationBilling.Value;

                // SQL: Override only if current is NULL or is a DEFAULT_BUSINESS_BILLING_MODEL
                if (!currentBillingModelId.HasValue || await IsDefaultBusinessBillingModelAsync(currentBillingModelId.Value, ct))
                {
                    _logger.LogInformation(
                        "ApplyLocationBasedBillingModelAsync: applying location-based billing {BillingModel} (attribute {Attr})",
                        billingModelId, licenseAttributeId);

                    return (billingModelId, licenseAttributeId);
                }

                _logger.LogDebug(
                    "ApplyLocationBasedBillingModelAsync: current billing model {Current} not null/default; keeping input",
                    currentBillingModelId);
                return (currentBillingModelId, currentLicenseAttributeId);
            }

            // No location-specific entry: use DEFAULT_BUSINESS_BILLING_MODEL
            _logger.LogDebug(
                "ApplyLocationBasedBillingModelAsync: no location-specific entry; querying default business model");

            var defaultBilling = await _repository.GetBusinessDefaultBillingModelAsync(ct);
            if (defaultBilling.HasValue)
            {
                var (licenseAttributeId, billingModelId) = defaultBilling.Value;

                // SQL: SELECT @license_attribute_license_value = ISNULL(@license_attribute_license_value, ...)
                var finalBillingModel = currentBillingModelId ?? billingModelId;
                var finalAttributeId = currentLicenseAttributeId ?? licenseAttributeId;

                _logger.LogInformation(
                    "ApplyLocationBasedBillingModelAsync: applying business default billing {BillingModel} (attribute {Attr})",
                    finalBillingModel, finalAttributeId);

                return (finalBillingModel, finalAttributeId);
            }

            _logger.LogDebug("ApplyLocationBasedBillingModelAsync: no default found; using input");
            return (currentBillingModelId, currentLicenseAttributeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ApplyLocationBasedBillingModelAsync: error resolving location-based billing; using input");
            return (currentBillingModelId, currentLicenseAttributeId);
        }
    }

    /// <summary>Helper: Check if a billing model is in the DEFAULT_BUSINESS_BILLING_MODEL set.</summary>
    private async Task<bool> IsDefaultBusinessBillingModelAsync(int billingModelId, CancellationToken ct)
    {
        // TODO: Check if billingModelId is in DEFAULT_BUSINESS_BILLING_MODEL config
        // For now, assume common business defaults: {110, 12, ...}
        var defaultBusinessModels = new[] { 110, 12, 111, 112, 210, 211, 212, 13, 113, 213 };
        return defaultBusinessModels.Contains(billingModelId);
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.11: Global LicenseAttributeLicenseValue Fallback from Existing Items
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.11: Fallback global billing model from existing primary cart item.
    ///
    /// SQL equivalent:
    /// <code>
    /// IF @license_attribute_license_value IS NULL
    /// BEGIN
    ///   SELECT @license_attribute_id = e.license_attribute_id,
    ///          @license_attribute_license_value = e.license_attribute_license_value
    ///   FROM @existing_item_table e
    ///   WHERE e.item_hierarchy_id = 1
    /// END
    /// </code>
    ///
    /// Purpose: If no billing model is provided in the incoming bundle JSON, fallback to
    /// the existing cart's primary item billing model (scalar variable fallback).
    ///
    /// Non-fatal: Returns input if existing items unavailable or no primary item found.
    /// </summary>
    /// <param name="preparedModel">Prepared model containing ExistingItems loaded in Section 1.8.</param>
    /// <param name="currentBillingModelId">Current global billing model (may be null).</param>
    /// <param name="currentLicenseAttributeId">Current global license attribute ID (may be null).</param>
    /// <returns>Resolved (billingModelId, licenseAttributeId), or input if not found.</returns>
    public (int? BillingModelId, int? LicenseAttributeId) ApplyGlobalBillingModelFallback(
        CartOrderPreparedModel preparedModel,
        int? currentBillingModelId,
        int? currentLicenseAttributeId)
    {
        if (preparedModel is null)
        {
            throw new ArgumentNullException(nameof(preparedModel));
        }

        // SQL guard: IF @license_attribute_license_value IS NULL
        if (currentBillingModelId.HasValue)
        {
            _logger.LogDebug(
                "ApplyGlobalBillingModelFallback: fallback not executed because billing model is already populated ({Model})",
                currentBillingModelId);
            return (currentBillingModelId, currentLicenseAttributeId);
        }

        _logger.LogDebug("ApplyGlobalBillingModelFallback: fallback executed (billing model is null)");

        var existingItems = preparedModel.ExistingItems;

        // Early return: no existing items available
        if (existingItems is null || existingItems.Count == 0)
        {
            _logger.LogDebug("ApplyGlobalBillingModelFallback: primary item not found (ExistingItems is empty)");
            return (currentBillingModelId, currentLicenseAttributeId);
        }

        _logger.LogDebug(
            "ApplyGlobalBillingModelFallback: looking for primary existing item (ItemHierarchyId=1)");

        try
        {
            // SQL: WHERE e.item_hierarchy_id = 1 (primary item)
            var primaryExisting = existingItems.FirstOrDefault(i => i.ItemHierarchyId == 1);

            if (primaryExisting is null)
            {
                _logger.LogDebug(
                    "ApplyGlobalBillingModelFallback: primary item not found");
                return (currentBillingModelId, currentLicenseAttributeId);
            }

            _logger.LogDebug(
                "ApplyGlobalBillingModelFallback: primary item found (LineItem={LineItem})",
                primaryExisting.LineItem);

            var copiedBillingModel = primaryExisting.LicenseAttributeLicenseValue;
            var copiedLicenseAttributeId = currentLicenseAttributeId ?? primaryExisting.LicenseAttributeId;

            if (!copiedBillingModel.HasValue && !copiedLicenseAttributeId.HasValue)
            {
                _logger.LogDebug(
                    "ApplyGlobalBillingModelFallback: primary item has no attribute values to copy");
                return (currentBillingModelId, currentLicenseAttributeId);
            }

            _logger.LogDebug(
                "ApplyGlobalBillingModelFallback: values copied from primary item (LicenseAttributeId={LicenseAttributeId}, LicenseAttributeLicenseValue={LicenseAttributeLicenseValue})",
                copiedLicenseAttributeId, copiedBillingModel);

            return (copiedBillingModel, copiedLicenseAttributeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ApplyGlobalBillingModelFallback: error applying fallback; using input");
            return (currentBillingModelId, currentLicenseAttributeId);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.15: Storage GB Default Calculation
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.15: Enrichment of default storage GB for items using SQL fn_get_item_storage_gb equivalent.
    ///
    /// SQL equivalent:
    /// <code>
    /// UPDATE @item_table
    /// SET storage_gb = dbo.fn_get_item_storage_gb(
    ///     quantity, license_category_name, DEFAULT,
    ///     usage_pricing_model_id, DEFAULT, DEFAULT)
    /// WHERE storage_gb IS NULL
    /// </code>
    ///
    /// Purpose: Resolve storage via repository call to preserve SQL control flow.
    /// Service does not implement fn_get_item_storage_gb rules.
    ///
    /// Non-fatal: Items without resolved storage are left unchanged.
    /// </summary>
    /// <param name="items">Items to enrich (mutated in-place).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task EnrichDefaultStorageGbWithRepositoryAsync(
        List<CartOrderItemContext> items,
        CancellationToken ct = default)
    {
        if (items is null || items.Count == 0)
        {
            _logger.LogDebug("EnrichDefaultStorageGbWithRepositoryAsync: no items to enrich");
            return;
        }

        _logger.LogDebug(
            "EnrichDefaultStorageGbWithRepositoryAsync: enriching {Count} items with default storage",
            items.Count);

        // TODO: Replace Repository.GetItemStorageGbAsync with the actual SQL
        // fn_get_item_storage_gb implementation once the SQL function definition
        // and full database access are available.

        int enriched = 0;

        foreach (var item in items)
        {
            // SQL: WHERE storage_gb IS NULL
            if (item.StorageGb.HasValue)
                continue;

            try
            {
                // Resolve through repository (service-layer does not compute storage rules)
                int? resolvedStorage = await _repository.GetItemStorageGbAsync(
                    item.Quantity,
                    item.LicenseCategoryName ?? "(unknown)",
                    item.UsagePricingModelId,
                    ct);

                if (resolvedStorage.HasValue)
                {
                    item.StorageGb = resolvedStorage;
                    enriched++;

                    _logger.LogDebug(
                        "EnrichDefaultStorageGbWithRepositoryAsync: LineItem={Line} Category={Category} Qty={Qty} → {Storage}GB",
                        item.LineItem, item.LicenseCategoryName, item.Quantity, resolvedStorage);
                }
                else
                {
                    _logger.LogDebug(
                        "EnrichDefaultStorageGbWithRepositoryAsync: LineItem={Line} repository returned null; StorageGb unchanged",
                        item.LineItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "EnrichDefaultStorageGbWithRepositoryAsync: error resolving storage for LineItem={Line}",
                    item.LineItem);
            }
        }

        _logger.LogInformation(
            "EnrichDefaultStorageGbWithRepositoryAsync: enriched {Count} of {Total} items",
            enriched, items.Count);
    }

    /// <summary>
    /// SECTION 1.2.2: Apply currency fallback using partner configuration.
    ///
    /// SQL equivalent:
    /// <code>
    /// IF @currency_id IS NULL AND @partner_id IS NOT NULL
    /// BEGIN
    ///   SELECT @currency_code = config_value
    ///   FROM partner_configuration pc
    ///   WHERE pc.partner_id = @partner_id
    ///     AND pc.partner_configuration_id = 15
    ///
    ///   IF @currency_code IS NOT NULL
    ///     SELECT @currency_id = c.currency_id
    ///     FROM currency c WHERE c.currency_code = @currency_code
    /// END
    ///
    /// IF @currency_id IS NULL
    ///   SELECT @currency_id = 1  -- USD default
    /// </code>
    ///
    /// Non-fatal: logs warnings and falls back to USD (1) if lookup fails.
    /// </summary>
    /// <param name="userContext">User context to update with resolved currency.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task ApplyCurrencyFallbackAsync(
        CartOrderUserContext userContext,
        CancellationToken ct = default)
    {
        if (userContext is null)
        {
            _logger.LogDebug("ApplyCurrencyFallbackAsync: userContext is null");
            return;
        }

        // Already resolved?
        if (userContext.CurrencyId.HasValue)
        {
            _logger.LogDebug(
                "ApplyCurrencyFallbackAsync: currency already resolved to {CurrencyId}",
                userContext.CurrencyId);
            return;
        }

        // No partner context?
        if (!userContext.PartnerId.HasValue)
        {
            _logger.LogDebug("ApplyCurrencyFallbackAsync: no partner ID, applying default USD");
            userContext.CurrencyId = 1;  // USD default
            return;
        }

        _logger.LogDebug(
            "ApplyCurrencyFallbackAsync: attempting partner configuration fallback for PartnerId={PartnerId}",
            userContext.PartnerId);

        try
        {
            // TODO: REPLACE WITH ACTUAL partner_configuration lookup
            // Query partner_configuration WHERE partner_id = @partner_id AND partner_configuration_id = 15
            // For now, use a placeholder that returns null (forces USD default)
            string? partnerCurrencyCode = null;

            if (!string.IsNullOrWhiteSpace(partnerCurrencyCode))
            {
                var currencyId = await _repository.LookupCurrencyIdByCodeAsync(partnerCurrencyCode, ct);

                if (currencyId.HasValue)
                {
                    userContext.CurrencyId = currencyId;
                    _logger.LogInformation(
                        "ApplyCurrencyFallbackAsync: PartnerId={PartnerId} config resolved to {Code} (CurrencyId={Id})",
                        userContext.PartnerId, partnerCurrencyCode, currencyId);
                    return;
                }
            }

            _logger.LogDebug(
                "ApplyCurrencyFallbackAsync: partner configuration lookup for PartnerId={PartnerId} returned no currency code",
                userContext.PartnerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ApplyCurrencyFallbackAsync: error in partner configuration lookup for PartnerId={PartnerId}",
                userContext.PartnerId);
        }

        // Fall back to USD (1)
        userContext.CurrencyId = 1;
        _logger.LogInformation("ApplyCurrencyFallbackAsync: applying default CurrencyId=1 (USD)");
    }

    // ══════════════════════════════════════════════════════════════════════════════════
    // SECTION 1.6: Return Prepared Model
    // ══════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// SECTION 1.6: Assemble complete preparation result
    /// 
    /// Purpose: Combine all loaded data (cart, context, license, items, products)
    /// into a single prepared model that downstream sections can work with.
    /// 
    /// Returns: CartOrderPreparedModel containing all loaded and organized data
    /// </summary>
    public CartOrderPreparedModel AssemblePreparedModel(
        Models.Responses.CartOrderResponse? cart,
        CartOrderUserContext context,
        CartOrderLicenseContext? license,
        List<CartOrderItemContext> items,
        Dictionary<int, CartOrderProductContext> products)
    {
        _logger.LogDebug("Assembling prepared model: Cart={HasCart}, Items={ItemCount}, Products={ProductCount}",
            cart?.VendorOrderCode ?? "NEW", items.Count, products.Count);

        var model = new CartOrderPreparedModel
        {
            Cart = cart,
            UserContext = context,
            License = license,
            Items = items,
            Products = products,
            PreparedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Prepared model assembled: VendorCode={Code}, Items={Items}, Products={Products}",
            cart?.VendorOrderCode ?? "NEW", items.Count, products.Count);

        return model;
    }
}

// ══════════════════════════════════════════════════════════════════════════════════
// Section 1 Data Models
// ══════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Prepared user/account context extracted from the request.
/// Contains all normalized fields needed for downstream processing.
/// </summary>
public class CartOrderUserContext
{
    // Order-level context
    public string SiteId { get; set; } = default!;
    public string Locale { get; set; } = default!;
    public string UserIp { get; set; } = default!;

    // Account context (from middleware)
    public int? CsiUserId { get; set; }
    public string? AccountUserName { get; set; }
    public string? PartnerRateCode { get; set; }
    public string? TrxRc { get; set; }

    // Optional fields
    public string? PartnerKey { get; set; }
    public string? CurrencyCode { get; set; }
    public string? VendorOrderCode { get; set; }
    public string? RoutingAction { get; set; }
    public string? UrlLink { get; set; }

    // Resolved IDs (populated by ResolvePartnerIdAsync / ResolveCurrencyIdAsync)
    /// <summary>Resolved partner_id. Null when PartnerKey is absent or unrecognised.</summary>
    public int? PartnerId { get; set; }
    /// <summary>Resolved currency_id. Defaults to 1 (USD) after all fallbacks.</summary>
    public int? CurrencyId { get; set; }

    // Campaign context
    public int? MessageCampaignId { get; set; }
    public string? MessageCampaignPlatform { get; set; }
    public string? Key { get; set; }
    public int? CartDiscountId { get; set; }

    // Timestamp
    public DateTime SalesOrderDate { get; set; }
    public DateTime LoadedAt { get; set; }
}

/// <summary>
/// Prepared license context loaded from keycode lookup and license profile.
/// Translates fn_license_select_license_profile() result.
/// </summary>
public class CartOrderLicenseContext
{
    public int LicenseId { get; set; }
    public string Keycode { get; set; } = default!;
    public string? LicenseStatus { get; set; }
    public string? LicenseCategory { get; set; }
    public string? LicenseCategoryName { get; set; }
    public int? LicenseSeats { get; set; }
    public DateTime? ExpirationDate { get; set; }
    /// <summary>Category type: 'trial' or 'full'. CRITICAL for Section 2.1-2.3 date/type determination.</summary>
    public string? CategoryTypeName { get; set; }
    /// <summary>Billing model code. Needed for Section 2.1 (monthly billing rules).</summary>
    public int? LicenseAttributeLicenseValue { get; set; }
    /// <summary>Billing model from the bundle/cart. Used in Section 2.1.1 to detect billing model switches.</summary>
    public int? LicenseAttributeLicenseValueFromLicense { get; set; }
    /// <summary>Auto-renewal cycle in years. CRITICAL for Section 2.2 WIFI upgrade detection.</summary>
    public decimal? AutorenewCycle { get; set; }
    /// <summary>Retention model ID. Needed for Section 2.2.2 (retention model upgrade rules).</summary>
    public byte? RetentionModelId { get; set; }
    /// <summary>Retention term in years. Needed for Section 2.2.2 (retention model upgrade comparisons).</summary>
    public byte? RetentionTerm { get; set; }
    /// <summary>Usage pricing model ID (e.g., 2=Capacity). Needed for Section 2.3.3 storage calculations.</summary>
    public byte? UsagePricingModelId { get; set; }
    /// <summary>Storage GB from license. Needed for Section 2.3.3 storage upgrade calculations.</summary>
    public int? StorageGb { get; set; }
    /// <summary>Product platform ID. From license table (Section 1.3.1).</summary>
    public byte? ProductPlatformId { get; set; }
    /// <summary>License keycode type ID. From license table (Section 1.3.1).</summary>
    public int? LicenseKeycodeTypeId { get; set; }
    /// <summary>License distribution method ID. From license table (Section 1.3.1).</summary>
    public int? LicenseDistributionMethodId { get; set; }
    public DateTime LoadedAt { get; set; }
}

/// <summary>
/// Prepared item context from existing cart.
/// </summary>
public class CartOrderItemContext
{
    public int CartOrderItemId { get; set; }
    public int CartOrderId { get; set; }
    public int LineItem { get; set; }
    public int ProductId { get; set; }
    public string? LicenseCategoryName { get; set; }
    public int Quantity { get; set; }
    public int? LicenseSeats { get; set; }
    /// <summary>Total license seats for this item (initialized from license_seats). Used in Section 1.6 for upgrade seat calculation.</summary>
    public int? TotalLicenseSeats { get; set; }
    public int? StorageGb { get; set; }
    public decimal? Years { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    /// <summary>Vendor-supplied expiration date override. Used for WIFI items where Apple/Google provides the date. (Section 2.1.2)</summary>
    public DateTime? VendorExpirationDate { get; set; }
    /// <summary>license_attribute_id from existing cart item lookup (Section 1.11 fallback).</summary>
    public int? LicenseAttributeId { get; set; }
    public int? LicenseAttributeLicenseValue { get; set; }
    public string? Keycode { get; set; }
    /// <summary>Item hierarchy: 1 = primary product, 2 = secondary/add-on. Used in Section 2.1+ to determine processing rules.</summary>
    public int? ItemHierarchyId { get; set; }
    /// <summary>Product type: 1 = New, 2 = Renewal, 3 = Upgrade. Computed in Section 2.2.</summary>
    public int? ProductTypeId { get; set; }

    /// <summary>Retention model ID. Loaded from license profile for Section 2.2.2 rules.</summary>
    public byte? RetentionModelId { get; set; }
    /// <summary>Retention term in years. Loaded from license profile for Section 2.2.2 comparisons.</summary>
    public byte? RetentionTerm { get; set; }
    /// <summary>Usage pricing model ID. Loaded from license profile.</summary>
    public byte? UsagePricingModelId { get; set; }
    /// <summary>Product platform ID. Loaded from license profile.</summary>
    public byte? ProductPlatformId { get; set; }
    /// <summary>License keycode type ID.</summary>
    public int? LicenseKeycodeTypeId { get; set; }
    /// <summary>Bundle ID. Used in Section 2.1.1 to link secondary items to primary items for date inheritance.</summary>
    public int? CartItemBundleId { get; set; }
    /// <summary>Billing model from current license. Loaded from license for Section 2.1 and 2.3.1 billing restrictions.</summary>
    public int? LicenseAttributeLicenseValueFromLicense { get; set; }
    /// <summary>License category ID. Section 1.4.2 lookup value.</summary>
    public int? LicenseCategoryId { get; set; }
    /// <summary>Amended contract identifier. When set, bypasses unit override logic in Section 1.6 for amended upsell orders.</summary>
    public string? AmendedContract { get; set; }
    public DateTime LoadedAt { get; set; }
}

/// <summary>
/// Prepared product context with category information.
/// </summary>
public class CartOrderProductContext
{
    public int ProductId { get; set; }
    public string? Description { get; set; }
    public int? ProductTypeId { get; set; }
    public int? ProductFamilyId { get; set; }
    public int? ProductLineId { get; set; }
    public int? ProductLifecycleId { get; set; }
    public int? LicenseKeycodeTypeId { get; set; }
    public int? LicenseCategoryId { get; set; }
    public string? LicenseCategoryName { get; set; }
    public string? LicenseCategoryDescription { get; set; }
    public DateTime LoadedAt { get; set; }
}

/// <summary>
/// Complete prepared model assembled from all Section 1 operations.
/// This is passed to downstream sections for processing.
/// 
/// ISSUE #11 NOTE - Multi-Category Order Support:
/// The SQL @license_table can contain multiple rows (one per license_category_name).
/// Current limitation: CartOrderLicenseContext is a single object, supporting only one license.
/// 
/// Migration Path for Multi-Category Support:
/// 1. Change License property to List&lt;CartOrderLicenseContext&gt;
/// 2. Update ProductDeterminationService methods to iterate through licenses by category:
///    - DeterminePrimaryProductDatesAsync: Filter license by item.LicenseCategoryName
///    - DetermineSecondaryProductDatesAsync: Filter license by category
///    - Helper methods: Accept optional category parameter
/// 3. Update repository to load all licenses (not just first by keycode)
/// 4. Test with multi-category orders (e.g., Backup + Endpoint + WIFI)
/// 
/// Current Implementation: Single-license-per-order (sufficient for current requirements)
/// </summary>
public class CartOrderPreparedModel
{
    public Models.Responses.CartOrderResponse? Cart { get; set; }
    public CartOrderUserContext UserContext { get; set; } = default!;
    /// <summary>
    /// Primary license context. Loaded by keycode for the license category.
    /// 
    /// For multi-category orders, this represents one license; see ISSUE #11 above.
    /// </summary>
    public CartOrderLicenseContext? License { get; set; }
    /// <summary>New items being added to the cart (from request JSON).</summary>
    public List<CartOrderItemContext> Items { get; set; } = new();
    /// <summary>Existing items already in the cart (loaded in Section 1.8 from database). Used as fallback in secondary product date determination (Rule 6).</summary>
    public List<CartOrderItemContext> ExistingItems { get; set; } = new();
    public Dictionary<int, CartOrderProductContext> Products { get; set; } = new();
    /// <summary>Site ID (e.g., 'SFDC', 'webroot'). Loaded from cart_order (Section 1.2). Needed for Section 2.2.1 and 2.2.2 upgrade rules.</summary>
    public string? SiteId { get; set; }
    /// <summary>Product line ID. Loaded from license or license_category_product_line (Section 1.9).</summary>
    public int? ProductLineId { get; set; }
    /// <summary>Billing model from bundle JSON. Needed for Section 2.2.1 monthly billing checks.</summary>
    public int? BillingModelId { get; set; }
    /// <summary>Global billing model (@license_attribute_license_value). Needed for Section 2.3.1 billing restrictions.</summary>
    public int? GlobalBillingModelId { get; set; }
    /// <summary>Language code from locale mapping. Needed for Section 1.9 product line lookup.</summary>
    public string? LanguageCode { get; set; }
    /// <summary>Location code from locale mapping. Needed for Section 1.9 product line lookup.</summary>
    public string? LocationCode { get; set; }
    /// <summary>License ID from license table. Used for lookups (Section 1.3.1).</summary>
    public int? LicenseId { get; set; }
    /// <summary>Next process date for monthly billing conversions. From license_message (Section 1.3.3).</summary>
    public DateTime? NextProcessDate { get; set; }
    /// <summary>Partner ID from cart_order_partner. For audit/context.</summary>
    public int? PartnerId { get; set; }
    /// <summary>Has utility flag. Populated from UTILITY_BILLING_MODELS config (Section 1.3).</summary>
    public bool HasUtility { get; set; }
    public DateTime PreparedAt { get; set; }
}

// ══════════════════════════════════════════════════════════════════════════════════
// Bundle / Item JSON Models
// ══════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Strongly-typed output of <see cref="CartOrderPreparationService.ParseBundleJsonAsync"/>.
///
/// Represents the decoded content of the SQL <c>@bundle_json</c> parameter passed to
/// <c>usp_cart_insert_cart_order_item</c>.  All fields are optional so that callers can
/// distinguish "field absent in JSON" from "field present but null".
/// </summary>
public sealed class BundleContext
{
    /// <summary>License keycode for the bundle (SQL: <c>keycode</c>).</summary>
    public string? Keycode { get; set; }

    /// <summary>
    /// Billing model code from the bundle (SQL: <c>license_attribute_license_value</c>).
    /// Examples: 110 = annual, 12 = monthly.
    /// CRITICAL: Used in Section 1.7 monthly-to-annual conversion and Section 2.3.1 billing restrictions.
    /// </summary>
    public int? LicenseAttributeLicenseValue { get; set; }

    /// <summary>License keycode type discriminator (SQL: <c>license_keycode_type_id</c>).</summary>
    public int? LicenseKeycodeTypeId { get; set; }

    /// <summary>
    /// Identifies the requested operation (SQL: <c>order_item_update_type_id</c>).
    /// Typical values: 1 = Insert, 2 = Update, 3 = Delete.
    /// Defaults to 1 (Insert) when null (SQL: ISNULL(@order_item_update_type_id, 1)).
    /// </summary>
    public int OrderItemUpdateTypeId { get; set; } = 1;

    /// <summary>Pricing tier applied to bundle products (SQL: <c>product_pricing_level_id</c>).</summary>
    public int? ProductPricingLevelId { get; set; }

    /// <summary>Optional discount ID to apply to the order (SQL: <c>cart_discount_id</c>).</summary>
    public int? CartDiscountId { get; set; }

    /// <summary>License lookup key / message key (SQL: <c>key</c>).</summary>
    public string? MessageKey { get; set; }

    /// <summary>
    /// Utility flag (SQL: <c>@has_utility</c>). True when billing model is in the UTILITY_BILLING_MODELS set (Section 1.3).
    /// Used to control pricing display and discount logic in Section 4.1.7.
    /// </summary>
    public bool HasUtility { get; set; }
}

/// <summary>
/// One row of the SQL <c>@unit_override</c> table variable, produced by Section 1.5.1
/// for SFDC orders and consumed by Section 1.6 (upgrade seat calculation).
/// </summary>
public sealed class SfdcUnitOverride
{
    /// <summary>1-based line number matching <see cref="CartOrderItemContext.LineItem"/>.</summary>
    public int LineItem { get; set; }

    /// <summary>License category name (e.g., "SMB", "SOHO").</summary>
    public string? LicenseCategoryName { get; set; }

    /// <summary>
    /// Net new seats: <c>item.license_seats - license.license_seats</c>.
    /// Corresponds to the SQL column <c>@unit_override.license_seats</c>.
    /// </summary>
    public int DeltaLicenseSeats { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────────
// Internal JSON payload shapes  (not public API – only used for deserialization)
// ──────────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Internal deserialization target for <c>@bundle_json</c>.
/// Property names use snake_case to match the SQL JSON keys exactly.
/// </summary>
internal sealed class BundleJsonPayload
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    [JsonPropertyName("keycode")]
    public string? Keycode { get; set; }

    [JsonPropertyName("license_attribute_license_value")]
    public int? LicenseAttributeLicenseValue { get; set; }

    [JsonPropertyName("license_keycode_type_id")]
    public int? LicenseKeycodeTypeId { get; set; }

    [JsonPropertyName("order_item_update_type_id")]
    public int? OrderItemUpdateTypeId { get; set; }

    [JsonPropertyName("product_pricing_level_id")]
    public int? ProductPricingLevelId { get; set; }

    [JsonPropertyName("cart_discount_id")]
    public int? CartDiscountId { get; set; }

    /// <summary>Maps JSON key "key" to MessageKey.</summary>
    [JsonPropertyName("key")]
    public string? MessageKey { get; set; }
}

/// <summary>
/// Internal deserialization target for one element of the <c>@item_json</c> array.
/// Property names use snake_case to match the SQL JSON keys exactly.
/// </summary>
internal sealed class ItemJsonPayload
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }

    [JsonPropertyName("license_category_name")]
    public string? LicenseCategoryName { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("license_seats")]
    public int? LicenseSeats { get; set; }

    [JsonPropertyName("storage_gb")]
    public int? StorageGb { get; set; }

    [JsonPropertyName("years")]
    public decimal? Years { get; set; }

    [JsonPropertyName("start_date")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("expiration_date")]
    public DateTime? ExpirationDate { get; set; }

    [JsonPropertyName("vendor_expiration_date")]
    public DateTime? VendorExpirationDate { get; set; }

    [JsonPropertyName("keycode")]
    public string? Keycode { get; set; }

    [JsonPropertyName("item_hierarchy_id")]
    public int? ItemHierarchyId { get; set; }

    [JsonPropertyName("cart_item_bundle_id")]
    public int? CartItemBundleId { get; set; }

    [JsonPropertyName("license_attribute_license_value")]
    public int? LicenseAttributeLicenseValue { get; set; }

    [JsonPropertyName("usage_pricing_model_id")]
    public byte? UsagePricingModelId { get; set; }

    [JsonPropertyName("retention_model_id")]
    public byte? RetentionModelId { get; set; }

    [JsonPropertyName("retention_term")]
    public byte? RetentionTerm { get; set; }

    [JsonPropertyName("product_platform_id")]
    public byte? ProductPlatformId { get; set; }

    [JsonPropertyName("license_keycode_type_id")]
    public int? LicenseKeycodeTypeId { get; set; }

    [JsonPropertyName("amended_contract")]
    public string? AmendedContract { get; set; }
}
