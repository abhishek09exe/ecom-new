using ecom_new_api.Models.Requests;
using ecom_new_api.Services.Pricing;
using Microsoft.AspNetCore.Mvc;

namespace ecom_new_api.Controllers;

/// <summary>
/// GET /api/bundle-pricing — stateless read-only pricing endpoint.
/// Called by the configure page every time the user adjusts seats, years, or billing model.
/// </summary>
[ApiController]
public sealed class BundlePricingController : ControllerBase
{
    private readonly IPricingService _pricing;
    private readonly ILogger<BundlePricingController> _logger;

    public BundlePricingController(IPricingService pricing, ILogger<BundlePricingController> logger)
    {
        _pricing = pricing;
        _logger  = logger;
    }

    /// <summary>
    /// Returns pricing for one or more bundle items including sub-totals, discounts, and formatted amounts.
    /// </summary>
    /// <response code="200">Pricing calculated successfully.</response>
    /// <response code="400">Invalid request parameters.</response>
    /// <response code="422">Stored procedure returned no pricing rows for the given items.</response>
    /// <response code="500">Unexpected database or server error.</response>
    [HttpGet("/bundle-pricing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get([FromQuery] BundlePricingRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            _logger.LogInformation("BundlePricing request: locale={Locale}, items={ItemCount}, first item modules={ModuleCount}",
                request.Locale,
                request.Items.Count,
                request.Items.FirstOrDefault()?.Modules.Count ?? 0);

            foreach (var item in request.Items)
                _logger.LogInformation("  Item: {Cat} seats={Seats} years={Years} msgKey={Key} modules=[{Mods}]",
                    item.LicenseCategoryName, item.LicenseSeats, item.Years, item.MessageKey,
                    string.Join(", ", item.Modules.Select(m => m.LicenseCategoryName)));

            var result = await _pricing.GetBundlePricingAsync(request);

            _logger.LogInformation("BundlePricing result: {Count} items returned", result.Items.Count);

            if (!result.Items.Any())
                return UnprocessableEntity(new { error = "No pricing found for these items." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bundle pricing for locale={Locale}", request.Locale);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = ex.Message, type = ex.GetType().Name, detail = ex.InnerException?.Message });
        }
    }
}
