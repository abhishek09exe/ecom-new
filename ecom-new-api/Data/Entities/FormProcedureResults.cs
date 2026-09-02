using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Result row returned by <c>usp_form_insert_form_submit</c>.
/// Mirrors the legacy form\insert\FormSubmit procedure whose schema is
/// form_name, ip_address, form_url, form_json, insert_by.
/// </summary>
public sealed class FormSubmitInsertResult
{
    [Column("form_submit_id")]
    public long FormSubmitId { get; set; }
}

/// <summary>
/// Result row returned by <c>usp_form_insert_form_response</c>.
/// Legacy schema: form_submit_id, form_response.
/// </summary>
public sealed class FormResponseInsertResult
{
    [Column("form_response_id")]
    public long FormResponseId { get; set; }

    [Column("form_response_key")]
    public string? FormResponseKey { get; set; }
}

/// <summary>
/// Result row returned by <c>usp_trial_insert_trial_registration</c>.
/// Legacy schema: trial_registration_json.
/// </summary>
public sealed class TrialRegistrationInsertResult
{
    [Column("trial_registration_id")]
    public long? TrialRegistrationId { get; set; }

    [Column("keycode")]
    public string? Keycode { get; set; }

    [Column("license_id")]
    public long? LicenseId { get; set; }

    [Column("customer_id")]
    public long? CustomerId { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }
}
