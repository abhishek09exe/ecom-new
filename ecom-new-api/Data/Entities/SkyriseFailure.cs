using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecom_new_api.Data.Entities;

/// <summary>
/// Audit trail for failed Skyrise keycode-generation attempts, mirroring PHP's
/// SkyriseFailure::create() call (Section 3.6 of the Skyrise reference doc):
/// skyrise_request / skyrise_failure_message are persisted as JSON text.
/// CONFIRMED against QA DB (2026-xx): real table name is "skyrise_license_failure"
/// (NOT "skyrise_failure"), PK is "skyrise_license_failure_id", and the timestamp
/// column is "failure_date" (NOT "insert_date"). Table already exists in QA — no
/// DDL/migration needed.
/// </summary>
[Table("skyrise_license_failure")]
public sealed class SkyriseFailure
{
    [Key]
    [Column("skyrise_license_failure_id")]
    public int SkyriseLicenseFailureId { get; set; }

    [Column("skyrise_request")]
    public string SkyriseRequest { get; set; } = string.Empty;

    [Column("skyrise_failure_message")]
    public string SkyriseFailureMessage { get; set; } = string.Empty;

    [Column("failure_date")]
    public DateTime FailureDate { get; set; }
}
