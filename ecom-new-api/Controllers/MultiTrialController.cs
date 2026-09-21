using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Services;
using ecom_new_api.Services.MultiTrial;
using Microsoft.AspNetCore.Mvc;

namespace ecom_new_api.Controllers;

/// <summary>
/// Multi Trial (GSM net-new trial) submissions.
///
/// Legacy equivalent: POST /forms/submissions with
/// form_name = "BusinessTrialRegistration" and form_type = "MULTI".
/// </summary>
[ApiController]
[Route("multi-trial")]
public sealed class MultiTrialController : ControllerBase
{
    private readonly IMultiTrialSubmissionService _service;
    private readonly ILogger<MultiTrialController> _logger;

    public MultiTrialController(IMultiTrialSubmissionService service, ILogger<MultiTrialController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Creates the trial registration for a Multi Trial submission
    /// (primary product plus the selected secondary modules).
    /// </summary>
    [HttpPost("submissions")]
    [ProducesResponseType(typeof(ApiResponse<MultiTrialSubmissionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<MultiTrialSubmissionResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubmission(
        [FromBody] MultiTrialSubmissionRequest request,
        CancellationToken ct)
    {
        if (request is null)
            return BadRequest(ApiResponse<MultiTrialSubmissionResponse>.ValidationFailure(["request body is required"]));

        _logger.LogInformation(
            "Multi Trial submission request: form_type={FormType} license_category_name={Category} modules={ModuleCount}",
            request.FormType, request.LicenseCategoryName, request.Modules.Count);

        var result = await _service.SubmitAsync(request, ct);

        return result.Kind switch
        {
            ServiceResultKind.Ok => StatusCode(StatusCodes.Status201Created,
                ApiResponse<MultiTrialSubmissionResponse>.Success(result.Data!)),

            ServiceResultKind.ValidationError =>
                BadRequest(ApiResponse<MultiTrialSubmissionResponse>.ValidationFailure(result.ValidationErrors)),

            ServiceResultKind.NotFound =>
                NotFound(ApiResponse<MultiTrialSubmissionResponse>.Failure(result.ErrorMessage ?? "Not found")),

            _ => StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<MultiTrialSubmissionResponse>.Failure(result.ErrorMessage ?? "An unexpected error occurred"))
        };
    }
}
