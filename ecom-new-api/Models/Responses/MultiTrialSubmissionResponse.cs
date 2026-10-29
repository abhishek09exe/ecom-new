namespace ecom_new_api.Models.Responses;

using System.Text.Json.Serialization;

/// <summary>
/// Response for POST /forms/submissions.
///
/// Column source: the success result set of usp_trial_insert_trial_registration plus the
/// synchronous Skyrise keycode-generation result.
/// </summary>
public sealed class MultiTrialSubmissionResponse
{
    [JsonIgnore]
    public int TrialId { get; init; }

    /// <summary>trial_registration.trial_registration_id</summary>
    public int TrialRegistrationId { get; init; }

    /// <summary>Keycode returned by Skyrise when generation succeeds.</summary>
    public string? Keycode { get; init; }

    /// <summary>"create_master" for GSM (license_keycode_type_id = 3), otherwise "create_key".</summary>
    public string? RequestType { get; init; }

    /// <summary>Echo of the submitted form_type ("MULTI").</summary>
    public string FormType { get; init; } = default!;

    /// <summary>Primary product the registration was created for.</summary>
    public string? LicenseCategoryName { get; init; }

    /// <summary>license_category.license_category_id resolved by the stored procedure.</summary>
    public int? LicenseCategoryId { get; init; }

    /// <summary>Seat count recorded for the primary product.</summary>
    public int? LicenseSeats { get; init; }

    /// <summary>product_line.product_line_id resolved by the stored procedure.</summary>
    public int? ProductLineId { get; init; }

    /// <summary>Comma-separated "module_code=module_type" list for the primary category.</summary>
    public string? LicenseModuleList { get; init; }

    /// <summary>e.g. "enterprise,trial"</summary>
    public string? LicenseType { get; init; }

    /// <summary>license_type.license_type_id</summary>
    public int? LicenseTypeId { get; init; }

    /// <summary>Capability activation window in days.</summary>
    public int? CapabilityActivationDays { get; init; }

    /// <summary>Storage entitlement in GB, when the category has one.</summary>
    public int? StorageGb { get; init; }

    /// <summary>Distribution method code resolved (or echoed) by the stored procedure.</summary>
    public string? LicenseDistributionMethodCode { get; init; }

    /// <summary>Secondary products registered alongside the primary product.</summary>
    public List<MultiTrialModuleResponse> Modules { get; init; } = [];
}
