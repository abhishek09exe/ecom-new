using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Services;

/// <summary>
/// Orchestrates cart order creation and read operations.
/// Sits between the controller (HTTP concerns) and the repository (DB concerns).
/// All business logic — validation, quote-key pivot, vendor order code generation — lives here.
/// </summary>
public interface ICartOrderService
{
    /// <summary>
    /// Creates a new cart order, or updates an existing pending (quote) cart if the
    /// supplied key resolves to an existing one.
    /// Returns the fully hydrated order response (re-read after insert).
    /// </summary>
    Task<ServiceResult<CartOrderResponse>> CreateCartOrderAsync(
        CartOrderCreateRequest request, CancellationToken ct = default);

    /// <summary>
    /// Returns the full cart aggregate for the given vendor_order_code.
    /// Maps to usp_cart_select_cart_order + usp_cart_select_cart_order_item.
    /// </summary>
    Task<ServiceResult<CartOrderResponse>> GetCartOrderAsync(
        string vendorOrderCode, CancellationToken ct = default);
}
