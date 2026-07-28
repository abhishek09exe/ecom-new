using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories;

namespace ecom_new_api.Services;

/// <summary>
/// Core cart order service.
/// Validates input, handles the quote-key pivot, delegates DB work to ICartOrderRepository.
/// </summary>
public sealed class CartOrderService : ICartOrderService
{
    private readonly ICartOrderRepository _repo;
    private readonly ILogger<CartOrderService> _logger;

    // ── Allowed-value sets ──────────────────────────────────────────────────────
    // TODO: REPLACE WITH ACTUAL — load these from DB / configuration at startup
    // so they don't need a code change when the allowed values change.

    private static readonly HashSet<string> AllowedSiteIds =
        ["gsm", "webroot", "ecm", "WRCART", "ecom", "test", "default"]; // TODO: REPLACE WITH ACTUAL — from DB/config (G23)

    private static readonly HashSet<string> AllowedLicenseCategoryNames =
        ["SAEP", "SAAP", "SASP", "SOHO", "SMB", "ENT"]; // TODO: REPLACE WITH ACTUAL — from DB/config

    private static readonly HashSet<int> AllowedYears =
        [1, 2, 3]; // TODO: REPLACE WITH ACTUAL — from DB/config

    public CartOrderService(ICartOrderRepository repo, ILogger<CartOrderService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // ── POST /cart/cart-orders ──────────────────────────────────────────────────

    public async Task<ServiceResult<CartOrderResponse>> CreateCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default)
    {
        // 1) Validate
        var errors = ValidateCreateRequest(request);
        if (errors.Count > 0)
            return ServiceResult<CartOrderResponse>.Invalid(errors);

        // 2) Quote-key pivot: if the key already has a pending cart, update instead of insert
        //    TODO: REPLACE WITH ACTUAL — wire UpdateCartOrderAsync once available
        if (!string.IsNullOrWhiteSpace(request.Key))
        {
            var existingCode = await _repo.FindExistingVendorOrderCodeByKeyAsync(request.Key, ct);
            if (existingCode is not null)
            {
                _logger.LogInformation(
                    "Key {Key} resolved to existing cart {VendorOrderCode} — pivoting to update",
                    request.Key, existingCode);

                // TODO: REPLACE WITH ACTUAL — call usp_cart_update_cart_order here
                // For now fall through to insert so the endpoint keeps responding
                _logger.LogWarning(
                    "Quote-key update path not yet implemented — proceeding with insert as placeholder");
            }
        }

        // 3) Insert — returns vendor_order_code
        var vendorOrderCode = await _repo.InsertCartOrderAsync(request, ct);

        // 4) Re-read — the API response is the hydrated aggregate, NOT the raw insert output
        var order = await _repo.SelectCartOrderAsync(vendorOrderCode, ct);
        if (order is null)
        {
            _logger.LogError("SelectCartOrderAsync returned null after insert for code {Code}", vendorOrderCode);
            return ServiceResult<CartOrderResponse>.Error("Cart order created but could not be retrieved");
        }

        return ServiceResult<CartOrderResponse>.Ok(order);
    }
    // ── GET /cart/cart-orders/{vendorOrderCode} ────────────────────────────────────

    public async Task<ServiceResult<CartOrderResponse>> GetCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(vendorOrderCode))
            return ServiceResult<CartOrderResponse>.Invalid(["vendor_order_code is required"]);

        var order = await _repo.SelectCartOrderAsync(vendorOrderCode, ct);
        if (order is null)
            return ServiceResult<CartOrderResponse>.NotFound(
                $"Cart order '{vendorOrderCode}' not found");

        return ServiceResult<CartOrderResponse>.Ok(order);
    }
    // ── Validation ──────────────────────────────────────────────────────────────

    private List<string> ValidateCreateRequest(CartOrderCreateRequest r)
    {
        var errors = new List<string>();

        // Order-level rules (sourced from MIGRATION_STATUS_REPORT.md § Validation rules)

        if (string.IsNullOrWhiteSpace(r.SiteId))
            errors.Add("site_id is required");
        else if (!AllowedSiteIds.Contains(r.SiteId))
            errors.Add($"site_id '{r.SiteId}' is not in the allowed set");
        // TODO: REPLACE WITH ACTUAL — AllowedSiteIds loaded from DB/config

        if (string.IsNullOrWhiteSpace(r.Locale))
            errors.Add("locale is required");

        if (!string.IsNullOrWhiteSpace(r.CurrencyCode) && r.CurrencyCode.Length != 3)
            errors.Add("currency_code must be a valid ISO 4217 code (3 characters)");
        // TODO: REPLACE WITH ACTUAL — validate against currency table in DB

        // `!IsNullOrWhiteSpace` + `Trim().Length == 0` is a dead branch — rewritten
        // to correctly catch an explicitly-blank string such as "" or "   ".
        if (r.VendorOrderCode is not null && string.IsNullOrWhiteSpace(r.VendorOrderCode))
            errors.Add("vendor_order_code must not be blank if provided");

        if (r.MessageCampaignId.HasValue && r.MessageCampaignId <= 0)
            errors.Add("message_campaign_id must be a positive integer if provided");

        if (r.MessageCampaignPlatform is not null && string.IsNullOrWhiteSpace(r.MessageCampaignPlatform))
            errors.Add("message_campaign_platform must not be blank if provided");

        if (!string.IsNullOrWhiteSpace(r.PartnerKey) && !Guid.TryParse(r.PartnerKey, out _))
            errors.Add("partner_key must be a valid UUID if provided");

        if (r.AccountUserName is not null && string.IsNullOrWhiteSpace(r.AccountUserName))
            errors.Add("account_user_name must not be blank if provided");

        if (!string.IsNullOrWhiteSpace(r.UrlLink) &&
            !Uri.TryCreate(r.UrlLink, UriKind.Absolute, out _))
            errors.Add("url_link must be a valid absolute URL if provided");

        // Item-level rules
        foreach (var (item, index) in r.Items.Select((x, i) => (x, i)))
        {
            var prefix = $"items[{index}]";

            if (string.IsNullOrWhiteSpace(item.LicenseCategoryName))
                errors.Add($"{prefix}.license_category_name is required");
            else if (!AllowedLicenseCategoryNames.Contains(item.LicenseCategoryName))
                errors.Add($"{prefix}.license_category_name '{item.LicenseCategoryName}' is not in the allowed set");
            // TODO: REPLACE WITH ACTUAL — AllowedLicenseCategoryNames loaded from DB/config

            if (item.Quantity.HasValue && item.Quantity <= 0)
                errors.Add($"{prefix}.quantity must be positive if provided");

            if (item.LicenseSeats.HasValue && item.LicenseSeats <= 0)
                errors.Add($"{prefix}.license_seats must be positive if provided");

            if (item.Years.HasValue && !AllowedYears.Contains((int)item.Years.Value))
                errors.Add($"{prefix}.years must be in the allowed set ({string.Join(", ", AllowedYears)}) if provided");
            // TODO: REPLACE WITH ACTUAL — AllowedYears loaded from DB/config

            if (item.ItemHierarchyId.HasValue && item.ItemHierarchyId is not (1 or 2))
                errors.Add($"{prefix}.item_hierarchy_id must be 1 or 2 if provided");

            // TODO: REPLACE WITH ACTUAL — storage/seat compatibility check
            // StorageGb must be within configured maximums for the product/category.
            // Requires a DB lookup of product configuration — stub below.
            if (item.StorageGb.HasValue && item.StorageGb <= 0)
                errors.Add($"{prefix}.storage_gb must be positive if provided");
            // TODO: REPLACE WITH ACTUAL — validate StorageGb against product max via DB

            // TODO: REPLACE WITH ACTUAL — vault validation
            // VaultId (int) and Vault (array) must be validated against the configured vault list
            // for this product/category. Requires a DB lookup — no static check possible here.
        }

        return errors;
    }
}
