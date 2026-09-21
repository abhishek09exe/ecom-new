namespace ecom_new_api.Models.Requests;

/// <summary>
/// Request body for the Multi Trial (GSM net-new trial) submission.
///
/// Field names mirror the legacy Multi Trial payload
/// (form_name = "BusinessTrialRegistration", form_type = "MULTI") so the existing
/// frontend contract is preserved. Every field maps to either a
/// usp_trial_insert_trial_registration parameter or a node of trial_registration_json.
/// </summary>
public sealed class MultiTrialSubmissionRequest
{
    // ── Customer ───────────────────────────────────────────────────────────────

    /// <summary>customer.first_name — legacy "firstname".</summary>
    public string? FirstName { get; init; }

    /// <summary>customer.last_name — legacy "lastname".</summary>
    public string? LastName { get; init; }

    /// <summary>customer.customer_email — legacy "emailaddress".</summary>
    public string? CustomerEmail { get; init; }

    /// <summary>customer.company_name — legacy "company".</summary>
    public string? CompanyName { get; init; }

    /// <summary>
    /// customer.company_type_id — the stored procedure resolves company_type_name
    /// from this id via the company_type lookup table.
    /// </summary>
    public int? CompanyTypeId { get; init; }

    /// <summary>customer.company_type_name — only used when company_type_id is absent.</summary>
    public string? CompanyTypeName { get; init; }

    /// <summary>customer.phone_number — legacy "phonenumber".</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>customer.address_1</summary>
    public string? Address1 { get; init; }

    /// <summary>customer.address_2</summary>
    public string? Address2 { get; init; }

    /// <summary>customer.city</summary>
    public string? City { get; init; }

    /// <summary>customer.state — internal 2-character state code, not state_id.</summary>
    public string? State { get; init; }

    /// <summary>customer.postal_code</summary>
    public string? PostalCode { get; init; }

    /// <summary>customer.country</summary>
    public string? Country { get; init; }

    /// <summary>customer.opt_in — marketing email opt-in flag (0/1).</summary>
    public int? OptIn { get; init; }

    // ── Locale ─────────────────────────────────────────────────────────────────

    /// <summary>language_code — 2 characters, defaults to "en" (SP falls back to "EN").</summary>
    public string LanguageCode { get; init; } = "en";

    /// <summary>location_code — ISO2 or ISO3, defaults to "us" (SP converts ISO2 → ISO3).</summary>
    public string LocationCode { get; init; } = "us";

    // ── Primary product ────────────────────────────────────────────────────────

    /// <summary>Primary trial product, "SAEP" for Multi Trial. Becomes item_hierarchy_id = 1.</summary>
    public string? LicenseCategoryName { get; init; }

    /// <summary>Seat count for the primary product.</summary>
    public int LicenseSeats { get; init; }

    /// <summary>Trial duration in days. Legacy default is 30.</summary>
    public int TrialDays { get; init; } = 30;

    /// <summary>Contract years. The SP forces 0 when trial_days &gt; 0.</summary>
    public double? Years { get; init; }

    /// <summary>
    /// 3 = GSM master keycode (Multi Trial), 1 = standard key.
    /// Multi Trial always registers SAEP as the GSM parent.
    /// </summary>
    public int LicenseKeycodeTypeId { get; init; } = 3;

    /// <summary>Optional pre-resolved distribution method code; the SP resolves it when omitted.</summary>
    public string? LicenseDistributionMethodCode { get; init; }

    // ── Secondary modules ──────────────────────────────────────────────────────

    /// <summary>Selected secondary products (SDNS, SECA, OTEDR, OTMDR, …). Become item_hierarchy_id = 2.</summary>
    public List<MultiTrialModuleRequest> Modules { get; init; } = [];

    // ── Form metadata ──────────────────────────────────────────────────────────

    /// <summary>Legacy form name — "BusinessTrialRegistration".</summary>
    public string FormName { get; init; } = "BusinessTrialRegistration";

    /// <summary>Legacy form type — "MULTI" identifies the Multi Trial flow.</summary>
    public string FormType { get; init; } = "MULTI";

    // ── Partner ────────────────────────────────────────────────────────────────

    /// <summary>partner.partner_key</summary>
    public string? PartnerKey { get; init; }

    /// <summary>partner.account_user_name</summary>
    public string? AccountUserName { get; init; }

    /// <summary>partner.partner_account_code</summary>
    public string? PartnerAccountCode { get; init; }

    /// <summary>partner.partner_product_id</summary>
    public string? PartnerProductId { get; init; }

    // ── Salesforce identifiers (passed through, no integration in this slice) ──

    /// <summary>sfdc.sfdc_lead_id</summary>
    public string? SfdcLeadId { get; init; }

    /// <summary>sfdc.sfdc_opportunity_id</summary>
    public string? SfdcOpportunityId { get; init; }

    /// <summary>sfdc.sfdc_trial_id</summary>
    public string? SfdcTrialId { get; init; }

    /// <summary>sfdc.salesforce_campaign_id</summary>
    public string? SalesforceCampaignId { get; init; }

    /// <summary>sfdc.sfdc_distributor_id</summary>
    public string? SfdcDistributorId { get; init; }

    /// <summary>sfdc.sfdc_reseller_id</summary>
    public string? SfdcResellerId { get; init; }

    /// <summary>sfdc.salesforce_license_id</summary>
    public string? SalesforceLicenseId { get; init; }
}
