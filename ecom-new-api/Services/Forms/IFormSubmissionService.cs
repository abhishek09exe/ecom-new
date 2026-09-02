using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Services.Forms;

/// <summary>
/// Result of a form submission. Validation failures are keyed by field name because the
/// FormHandler client renders <c>responseJSON.errors[fieldName]</c> next to each input.
/// </summary>
public sealed class FormSubmissionResult
{
    public bool IsSuccess { get; init; }
    public FormSubmissionResponse? Data { get; init; }
    public Dictionary<string, List<string>> Errors { get; init; } = [];
    public string? ErrorMessage { get; init; }

    public static FormSubmissionResult Ok(FormSubmissionResponse data)
        => new() { IsSuccess = true, Data = data };

    public static FormSubmissionResult Invalid(Dictionary<string, List<string>> errors)
        => new() { IsSuccess = false, Errors = errors };

    public static FormSubmissionResult Invalid(string field, string message)
        => new() { IsSuccess = false, Errors = new() { [field] = [message] } };

    public static FormSubmissionResult Error(string message)
        => new() { IsSuccess = false, ErrorMessage = message };
}

public interface IFormSubmissionService
{
    Task<FormSubmissionResult> SubmitAsync(
        FormSubmissionRequest request,
        IDictionary<string, string?> rawFields,
        string? ipAddress,
        CancellationToken ct = default);
}
