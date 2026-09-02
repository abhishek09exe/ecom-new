namespace ecom_new_api.Models.Requests;

/// <summary>
/// Form payload for POST /forms/submissions.
///
/// Field set is ported from the www trial forms (form_trial_consumer, form_trial_business)
/// and the models that consume them:
///   - wr\models\trial\trial_ext\ConsumerTrialForm
///   - wr\models\trial\trial_ext\BusinessTrialForm
///   - wr\models\trial\TrialRegistration
/// </summary>
public sealed class FormSubmissionRequest
{
    // ── Form identity ──────────────────────────────────────────────────────────
    public string? FormName { get; set; }
    public string? FormType { get; set; }
    public string? FormUrl { get; set; }

    // ── Customer ───────────────────────────────────────────────────────────────
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? ConfirmCustomerEmail { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyTypeId { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string? OptIn { get; set; }

    // ── Locale ─────────────────────────────────────────────────────────────────
    public string? Locale { get; set; }
    public string? LanguageCode { get; set; }
    public string? LocationCode { get; set; }

    // ── Trial / license ────────────────────────────────────────────────────────
    public string? TrialDays { get; set; }
    public string? LicenseCategoryName { get; set; }
    public string? LicenseSeats { get; set; }
    public string? LicenseKeycodeTypeId { get; set; }
    public string? LicenseDistributionMethodCode { get; set; }
    public string? StorageGb { get; set; }
    public string? VaultId { get; set; }
    public string? UsagePricingModelId { get; set; }
    public string? ProductPlatformId { get; set; }
    public string? Modules { get; set; }

    // ── Partner ────────────────────────────────────────────────────────────────
    public string? PartnerKey { get; set; }
    public string? AccountUserName { get; set; }
    public string? PartnerAccountCode { get; set; }
    public string? PartnerProductId { get; set; }

    // ── Salesforce ─────────────────────────────────────────────────────────────
    public string? SfdcLeadId { get; set; }
    public string? SfdcOpportunityId { get; set; }
    public string? SfdcTrialId { get; set; }
    public string? SfdcResellerId { get; set; }
    public string? SfdcDistributorId { get; set; }
    public string? SalesforceCampaignId { get; set; }
    public string? SalesforceLicenseId { get; set; }

    // ── Marketing / tracking ───────────────────────────────────────────────────
    public string? LeadSource { get; set; }
    public string? SearchCampaign { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmTerm { get; set; }
    public string? UtmContent { get; set; }
}
