namespace ecom_new_api.Models.Responses;

/// <summary>
/// Success payload for POST /forms/submissions.
/// Shape is driven by the FormHandler client, which reads <c>form_response_key</c>
/// for the success redirect and <c>entity</c> for the confirmation page data.
/// </summary>
public sealed class FormSubmissionResponse
{
    public int ResponseCode { get; init; }
    public string FormResponseKey { get; init; } = default!;
    public long FormSubmitId { get; init; }
    public Dictionary<string, object?> Entity { get; init; } = [];
}

/// <summary>
/// Trial data hydrated after a successful trial registration insert, mirroring the
/// license/customer/profile blocks returned by the legacy trial registration flow.
/// </summary>
public sealed class TrialRegistrationResult
{
    public long? TrialRegistrationId { get; init; }
    public string? Keycode { get; init; }
    public string? LicenseCategoryName { get; init; }
    public int? LicenseSeats { get; init; }
    public int? TrialDays { get; init; }
    public string? StartDate { get; init; }
    public string? ExpirationDate { get; init; }
    public string? CustomerEmail { get; init; }
}
