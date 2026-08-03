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
    private readonly IConfiguration _config;

    public CartOrderService(ICartOrderRepository repo, ILogger<CartOrderService> logger, IConfiguration config)
    {
        _repo = repo;
        _logger = logger;
        _config = config;
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
        if (!string.IsNullOrWhiteSpace(request.MessageKey))
        {
            var existingCode = await _repo.FindExistingVendorOrderCodeByKeyAsync(request.MessageKey, ct);
            if (existingCode is not null)
            {
                _logger.LogInformation(
                    "Key {Key} resolved to existing cart {VendorOrderCode} — pivoting to update",
                    request.MessageKey, existingCode);

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

        // Build the route URL from routing_action + message_key
        if (!string.IsNullOrWhiteSpace(request.RoutingAction))
        {
            var baseUrl = _config["CartRouteBaseUrl"] ?? "https://www.webroot.com/us/en/cart";
            var routeUrl = $"{baseUrl}?routing_action={Uri.EscapeDataString(request.RoutingAction)}&key={request.MessageKey ?? string.Empty}";
            order.Route = new CartOrderRouteResponse { Route = routeUrl };
        }

        return ServiceResult<CartOrderResponse>.Ok(order);
    }

    // ── GET endpoints ───────────────────────────────────────────────────────────

    public async Task<ServiceResult<LicenseOptionsResponse>> GetLicenseOptionsAsync(
        string keycode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keycode))
            return ServiceResult<LicenseOptionsResponse>.Invalid(["keycode is required"]);

        var result = await _repo.SelectLicenseOptionsAsync(keycode, ct);
        return result is null
            ? ServiceResult<LicenseOptionsResponse>.NotFound($"No license found for keycode '{keycode}'")
            : ServiceResult<LicenseOptionsResponse>.Ok(result);
    }

    public async Task<ServiceResult<LicenseOptionsResponse>> GetLicenseOptionsByMessageKeyAsync(
        string messageKey, CancellationToken ct = default)
    {
        var keycode = await _repo.ResolveKeycodeFromMessageKeyAsync(messageKey, ct);
        if (keycode is null)
            return ServiceResult<LicenseOptionsResponse>.NotFound($"No license found for message_key '{messageKey}'");

        var result = await _repo.SelectLicenseOptionsAsync(keycode, ct);
        return result is null
            ? ServiceResult<LicenseOptionsResponse>.NotFound($"No license found for message_key '{messageKey}'")
            : ServiceResult<LicenseOptionsResponse>.Ok(result);
    }

    public async Task<ServiceResult<ConfigureResponse>> GetConfigureAsync(
        string keycode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keycode))
            return ServiceResult<ConfigureResponse>.Invalid(["keycode is required"]);

        var result = await _repo.SelectConfigureAsync(keycode, ct);
        return result is null
            ? ServiceResult<ConfigureResponse>.NotFound($"No configuration found for keycode '{keycode}'")
            : ServiceResult<ConfigureResponse>.Ok(result);
    }

    public async Task<ServiceResult<UpgradeResponse>> GetUpgradeAsync(
        string keycode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keycode))
            return ServiceResult<UpgradeResponse>.Invalid(["keycode is required"]);

        var result = await _repo.SelectUpgradeAsync(keycode, ct);
        return result is null
            ? ServiceResult<UpgradeResponse>.NotFound($"No upgrade options found for keycode '{keycode}'")
            : ServiceResult<UpgradeResponse>.Ok(result);
    }

    // ── Validation ──────────────────────────────────────────────────────────────

    private List<string> ValidateCreateRequest(CartOrderCreateRequest r)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(r.SiteId))
            errors.Add("site_id is required");

        if (string.IsNullOrWhiteSpace(r.Locale))
            errors.Add("locale is required");

        if (!string.IsNullOrWhiteSpace(r.CurrencyCode) && r.CurrencyCode.Length != 3)
            errors.Add("currency_code must be a valid ISO 4217 code (3 characters)");

        if (!string.IsNullOrWhiteSpace(r.PartnerKey) && !Guid.TryParse(r.PartnerKey, out _))
            errors.Add("partner_key must be a valid UUID if provided");

        if (!string.IsNullOrWhiteSpace(r.UrlLink) &&
            !Uri.TryCreate(r.UrlLink, UriKind.Absolute, out _))
            errors.Add("url_link must be a valid absolute URL if provided");

        if (r.MessageCampaignId.HasValue && r.MessageCampaignId <= 0)
            errors.Add("message_campaign_id must be a positive integer if provided");

        foreach (var (item, index) in r.Items.Select((x, i) => (x, i)))
        {
            var prefix = $"items[{index}]";

            if (string.IsNullOrWhiteSpace(item.LicenseCategoryName))
                errors.Add($"{prefix}.license_category_name is required");

            if (item.Quantity.HasValue && item.Quantity <= 0)
                errors.Add($"{prefix}.quantity must be positive if provided");

            if (item.LicenseSeats.HasValue && item.LicenseSeats <= 0)
                errors.Add($"{prefix}.license_seats must be positive if provided");

            if (item.ItemHierarchyId.HasValue && item.ItemHierarchyId is not (1 or 2))
                errors.Add($"{prefix}.item_hierarchy_id must be 1 or 2 if provided");

            if (item.StorageGb.HasValue && item.StorageGb <= 0)
                errors.Add($"{prefix}.storage_gb must be positive if provided");
        }

        return errors;
    }
}
