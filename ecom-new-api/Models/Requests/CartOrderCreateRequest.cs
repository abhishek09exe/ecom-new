namespace ecom_new_api.Models.Requests;

/// <summary>
/// Request body for POST /cart/cart-orders.
/// Maps to the PHP controller's accepted payload merged with account context injected by middleware.
/// </summary>
public sealed class CartOrderCreateRequest
{
    // ── Required fields ────────────────────────────────────────────────────────

    /// <summary>Identifies the ordering site (e.g. "gsm", "webroot"). Required.</summary>
    public string SiteId { get; init; } = default!;

    /// <summary>BCP-47 locale tag, e.g. "en-US". Required.</summary>
    public string Locale { get; init; } = default!;

    // ── Optional order-level fields ────────────────────────────────────────────

    /// <summary>ISO 4217 currency code. Falls back to partner config then default (USD) if omitted.</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>Existing vendor order code. Server generates one if omitted.</summary>
    public string? VendorOrderCode { get; init; }

    /// <summary>UUID of the partner placing the order.</summary>
    public string? PartnerKey { get; init; }

    /// <summary>Username on the partner account — injected by middleware from CSI user context.</summary>
    public string? AccountUserName { get; init; }

    /// <summary>Routing hint for downstream order processing (e.g. "autoprocess").</summary>
    public string? RoutingAction { get; init; }

    /// <summary>Sales order date override. Defaults to today if omitted.</summary>
    public DateTime? SalesOrderDate { get; init; }

    /// <summary>Campaign ID that drove this order.</summary>
    public int? MessageCampaignId { get; init; }

    /// <summary>Platform label for the campaign (e.g. "email", "web").</summary>
    public string? MessageCampaignPlatform { get; init; }

    /// <summary>
    /// Keycode / message key entered by the user.
    /// If this resolves to a quote key the service must pivot to an UPDATE rather than INSERT.
    /// </summary>
    public string? MessageKey { get; init; }

    /// <summary>Pre-applied discount identifier.</summary>
    public int? CartDiscountId { get; init; }

    /// <summary>Referral or landing URL.</summary>
    public string? UrlLink { get; init; }

    // ── Line items ─────────────────────────────────────────────────────────────

    /// <summary>Zero or more products being added to the cart in this request.</summary>
    public List<CartOrderItemRequest> Items { get; init; } = [];

    // ── Server-side injected fields (set by middleware, NOT trusted from client) ──

    /// <summary>
    /// Caller's IP address.
    /// TODO: REPLACE WITH ACTUAL — set this from HttpContext.Connection.RemoteIpAddress in middleware,
    /// never from the request body.
    /// </summary>
    public string UserIp { get; set; } = "0.0.0.0";

    /// <summary>
    /// CSI user ID resolved from X-CSI-USER-ID header.
    /// TODO: REPLACE WITH ACTUAL — injected by AuthMiddleware.
    /// </summary>
    public int? CsiUserId { get; set; }

    /// <summary>
    /// Partner rate code resolved from account context.
    /// TODO: REPLACE WITH ACTUAL — injected by AccountContextMiddleware.
    /// </summary>
    public string? PRc { get; set; }

    /// <summary>
    /// Transaction rate code resolved from account context.
    /// TODO: REPLACE WITH ACTUAL — injected by AccountContextMiddleware.
    /// </summary>
    public string? TrxRc { get; set; }
}
