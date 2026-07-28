using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ecom_new_api.Controllers;

/// <summary>
/// Handles the cart-orders API surface.
///
/// Middleware pipeline (not yet wired — see Program.cs TODOs):
///   1. Session/cart bootstrap     → injects vendor_order_code context
///   2. CSRF validation            → X-WRCART-CSRF header → 400 if missing on non-GET
///   3. Authentication             → X-CSI-USER / X-CSI-USER-ID headers → 401
///   4. Permission check           → cart_order.create → 403
///   5. Account context injection  → sets request.AccountUserName, CsiUserId, PRc, TrxRc
///   6. Locale injection           → X-CSI-LOCALE header → sets request.Locale if not in body
///
/// TODO: REPLACE WITH ACTUAL — add [Authorize] and permission attributes once
/// the auth middleware is implemented.
/// </summary>
[ApiController]
[Route("cart")]
public sealed class CartOrdersController : ControllerBase
{
    private readonly ICartOrderService _service;
    private readonly ILogger<CartOrdersController> _logger;

    public CartOrdersController(ICartOrderService service, ILogger<CartOrdersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── POST /cart/cart-orders ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a new cart order (or updates a pending quote cart if the key resolves to one).
    /// Called by the frontend JS when a user clicks "Add to Cart."
    /// </summary>
    [HttpPost("cart-orders")]
    [ProducesResponseType(typeof(ApiResponse<CartOrderResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CartOrderResponse>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCartOrder(
        [FromBody] CartOrderCreateRequest request,
        CancellationToken ct)
    {
        // Server-side inject user IP — never trust client-supplied value
        request.UserIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";

        // TODO: REPLACE WITH ACTUAL — once AuthMiddleware is wired, read these from
        // HttpContext.Items (populated by middleware) instead of setting defaults here.
        // request.CsiUserId = (int)HttpContext.Items["CsiUserId"]!;
        // request.AccountUserName = (string?)HttpContext.Items["AccountUserName"];
        // request.PRc  = (string?)HttpContext.Items["PRc"];
        // request.TrxRc = (string?)HttpContext.Items["TrxRc"];

        var result = await _service.CreateCartOrderAsync(request, ct);
        return MapResult(result, created: true);
    }

    // ── GET /cart/cart-orders/{vendorOrderCode} ─────────────────────────────────

    /// <summary>
    /// Returns the full cart aggregate for an existing order.
    /// Maps to usp_cart_select_cart_order + usp_cart_select_cart_order_item.
    /// </summary>
    [HttpGet("cart-orders/{vendorOrderCode}")]
    [ProducesResponseType(typeof(ApiResponse<CartOrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CartOrderResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCartOrder(
        [FromRoute] string vendorOrderCode,
        CancellationToken ct)
    {
        var result = await _service.GetCartOrderAsync(vendorOrderCode, ct);
        return MapResult(result);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private IActionResult MapResult<T>(ServiceResult<T> result, bool created = false)
    {
        return result.Kind switch
        {
            ServiceResultKind.Ok => created
                ? StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Success(result.Data!))
                : Ok(ApiResponse<T>.Success(result.Data!)),

            ServiceResultKind.ValidationError =>
                UnprocessableEntity(ApiResponse<T>.ValidationFailure(result.ValidationErrors)),

            ServiceResultKind.NotFound =>
                NotFound(ApiResponse<T>.Failure(result.ErrorMessage ?? "Not found")),

            _ => StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse<T>.Failure(result.ErrorMessage ?? "An unexpected error occurred"))
        };
    }
}
