using System.Text.Json;

namespace ecom_new_api.Models.Responses;

public sealed record LegacyFormSubmissionSnapshot
{
    public int FormSubmitId { get; init; }
    public string FormName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? FormUrl { get; init; }
    public string FormJson { get; init; } = string.Empty;
    public LegacyMultiTrialFormEntity Entity { get; init; } = new();
}

public sealed record LegacyFormSubmissionResponse
{
    public int FormSubmitId { get; init; }
    public string FormName { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? FormUrl { get; init; }
    public string FormJson { get; init; } = string.Empty;
    public LegacyMultiTrialFormEntity Entity { get; init; } = new();
    public string FormResponseKey { get; init; } = string.Empty;
}

public sealed record LegacyMultiTrialFormEntity
{
    public int FormSubmitId { get; init; }
    public string FormName { get; init; } = string.Empty;
    public string FormType { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public int? CompanyTypeId { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Country { get; init; }
    public string? State { get; init; }
    public string? LocationCode { get; init; }
    public int? LicenseId { get; init; }
    public int TrialRegistrationId { get; init; }
    public string? Keycode { get; init; }
    public string? LicenseCategoryName { get; init; }
    public int? LicenseCategoryId { get; init; }
    public int? LicenseSeats { get; init; }
    public int TrialDays { get; init; }
    public int LicenseKeycodeTypeId { get; init; }
    public string? LicenseDistributionMethodCode { get; init; }
    public string? LicenseType { get; init; }
    public int? LicenseTypeId { get; init; }
    public int? CapabilityActivationDays { get; init; }
    public string FormJson { get; init; } = string.Empty;
    public string? CampaignId { get; init; }
    public string? SalesforceCampaignId { get; init; }
    public LegacyMultiTrialFormTrial? Trial { get; init; }
    public List<MultiTrialModuleResponse> Modules { get; init; } = [];
}

public sealed record LegacyMultiTrialFormTrial
{
    public int TrialId { get; init; }
    public int TrialRegistrationId { get; init; }
    public string? LicenseKey { get; init; }
    public string Keycode { get; init; } = string.Empty;
}

public sealed record LegacyFormResponseLookupResponse
{
    public int FormResponseId { get; init; }
    public int FormSubmitId { get; init; }
    public string FormResponseKey { get; init; } = string.Empty;
    public JsonElement FormResponse { get; init; }
    public DateTime InsertDate { get; init; }
}