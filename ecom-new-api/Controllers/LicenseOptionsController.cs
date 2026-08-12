using ecom_new_api.Models.Responses;
using ecom_new_api.Services;
using ecom_new_api.Services.LicenseOptions;
using Microsoft.AspNetCore.Mvc;

namespace ecom_new_api.Controllers;

[ApiController]
public sealed class LicenseOptionsController : ControllerBase
{
    private readonly ILicenseOptionsService _service;

    public LicenseOptionsController(ILicenseOptionsService service) => _service = service;

    [HttpGet("/license-options")]
    [ProducesResponseType(typeof(ApiResponse<LicenseOptionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LicenseOptionsResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LicenseOptionsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLicenseOptions(
        [FromQuery] string? message_key,
        [FromQuery] string? locale,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message_key))
            return BadRequest(ApiResponse<LicenseOptionsResponse>.ValidationFailure(["message_key is required"]));

        if (!Guid.TryParse(message_key, out _))
            return BadRequest(ApiResponse<LicenseOptionsResponse>.ValidationFailure(["message_key must be a valid GUID"]));

        var result = await _service.GetLicenseOptionsByMessageKeyAsync(message_key, locale, ct);

        return result.Kind switch
        {
            ServiceResultKind.Ok => Ok(ApiResponse<LicenseOptionsResponse>.Success(result.Data!)),
            ServiceResultKind.NotFound => NotFound(ApiResponse<LicenseOptionsResponse>.Failure(result.ErrorMessage ?? "Not found")),
            ServiceResultKind.ValidationError => BadRequest(ApiResponse<LicenseOptionsResponse>.ValidationFailure(result.ValidationErrors)),
            _ => StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<LicenseOptionsResponse>.Failure(result.ErrorMessage ?? "An unexpected error occurred"))
        };
    }
}
