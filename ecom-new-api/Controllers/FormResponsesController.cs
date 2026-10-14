using System.Text.Json;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.Forms;
using Microsoft.AspNetCore.Mvc;

namespace ecom_new_api.Controllers;

[ApiController]
public sealed class FormResponsesController : ControllerBase
{
    private readonly IFormResponseRepository _repository;
    private readonly ILogger<FormResponsesController> _logger;

    public FormResponsesController(
        IFormResponseRepository repository,
        ILogger<FormResponsesController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("/forms/responses/{key}")]
    [ProducesResponseType(typeof(LegacyFormResponseLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LegacyFormResponseLookupResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFormResponse(string key, CancellationToken ct)
    {
        if (!Guid.TryParseExact(key, "D", out _))
            return NotFound(ApiResponse<LegacyFormResponseLookupResponse>.Failure("Form response not found"));

        try
        {
            var snapshot = await _repository.GetFormResponseAsync(key, ct);
            if (snapshot is null)
                return NotFound(ApiResponse<LegacyFormResponseLookupResponse>.Failure("Form response not found"));

            using var document = JsonDocument.Parse(snapshot.FormResponseJson);
            return Ok(new LegacyFormResponseLookupResponse
            {
                FormResponseId = snapshot.FormResponseId,
                FormSubmitId = snapshot.FormSubmitId,
                FormResponseKey = snapshot.FormResponseKey,
                FormResponse = document.RootElement.Clone(),
                InsertDate = snapshot.InsertDate
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (JsonException)
        {
            _logger.LogError("Stored form response is not valid JSON.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<LegacyFormResponseLookupResponse>.Failure("Form response could not be read"));
        }
        catch (Exception ex)
        {
            _logger.LogError("Form response lookup failed. ExceptionType={ExceptionType}", ex.GetType().Name);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<LegacyFormResponseLookupResponse>.Failure("Form response could not be read"));
        }
    }
}