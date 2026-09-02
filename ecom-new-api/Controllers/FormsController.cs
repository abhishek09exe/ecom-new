using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Services.Forms;
using Microsoft.AspNetCore.Mvc;

namespace ecom_new_api.Controllers;

/// <summary>
/// POST /forms/submissions — public trial-registration form endpoint.
/// Consumed by the WWW FormHandler, which expects a 200 payload containing
/// <c>form_response_key</c> and <c>entity</c>, or a 422 payload containing
/// field-keyed <c>errors</c>.
/// </summary>
[ApiController]
[Route("forms")]
public sealed class FormsController : ControllerBase
{
    private readonly IFormSubmissionService _forms;
    private readonly ILogger<FormsController> _logger;

    public FormsController(IFormSubmissionService forms, ILogger<FormsController> logger)
        => (_forms, _logger) = (forms, logger);

    /// <summary>Submits a consumer or business trial registration form.</summary>
    /// <response code="200">Trial registration created.</response>
    /// <response code="422">Validation failed; errors are keyed by field name.</response>
    /// <response code="500">Unexpected database or server error.</response>
    [HttpPost("submissions")]
    [Consumes("application/x-www-form-urlencoded", "multipart/form-data")]
    [ProducesResponseType(typeof(FormSubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Submit(
        [FromForm] FormSubmissionRequest request,
        CancellationToken ct)
    {
        if (!Request.HasFormContentType)
        {
            return UnprocessableEntity(new
            {
                errors = new Dictionary<string, List<string>>
                {
                    ["form_name"] = ["This field is required"]
                }
            });
        }

        // The legacy flow persists the full raw post, not just the mapped fields.
        var rawFields = Request.Form.Keys
            .ToDictionary(k => k, k => (string?)Request.Form[k].ToString(), StringComparer.OrdinalIgnoreCase);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var result = await _forms.SubmitAsync(request, rawFields, ipAddress, ct);

            if (!result.IsSuccess)
            {
                if (result.Errors.Count > 0)
                    return UnprocessableEntity(new { errors = result.Errors });

                _logger.LogError("Form submission failed: {Error}", result.ErrorMessage);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = result.ErrorMessage });
            }

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing form submission {FormName}", request.FormName);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "An unexpected error occurred processing the submission." });
        }
    }
}
