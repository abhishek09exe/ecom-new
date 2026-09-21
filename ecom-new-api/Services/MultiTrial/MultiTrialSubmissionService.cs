using System.Text.Json;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.Trials;
using ecom_new_api.Services.EmailVerification;
using ecom_new_api.Services.Skyrise;

namespace ecom_new_api.Services.MultiTrial;

/// <summary>
/// Multi Trial submission service — first backend slice.
///
/// Validates the legacy Multi Trial payload, builds trial_registration_json
/// (primary product at item_hierarchy_id = 1, selected modules at item_hierarchy_id = 2)
/// and delegates the insert to usp_trial_insert_trial_registration.
///
/// Out of scope for this slice (later Multi Trial stages): trial/license creation,
/// email queue and Salesforce/Marketo processing.
/// </summary>
public sealed class MultiTrialSubmissionService : IMultiTrialSubmissionService
{
    private readonly ITrialRegistrationRepository _repo;
    private readonly IEmailVerificationService _emailVerification;
    private readonly ISkyriseKeycodeClient _skyriseKeycodeClient;
    private readonly ILogger<MultiTrialSubmissionService> _logger;

    // Legacy PartnerCartTrial::MODULE_CATEGORYS
    private static readonly string[] ValidModuleCategories =
        ["SDNS", "SECA", "OTSF", "CBEP", "PLRM", "PLRCB", "PLRCS", "OTEDR", "OTMDR"];

    private static readonly int[] ValidTrialDays = [14, 30, 60, 90];
    private static readonly string[] PillrParentModules = ["OTEDR", "OTMDR"];
    private static readonly string[] PillrSubModules = ["PLRM", "PLRCB"];

    private const string PillrBaseProduct = "PLRP";
    private const string PillrComplianceStorage = "PLRCS";
    private const string MultiFormType = "MULTI";
    private const string TrialCategoryTypeName = "trial";
    private const int PrimaryHierarchyId = 1;
    private const int SecondaryHierarchyId = 2;
    private const int PillrRequiredTrialDays = 30;
    private const int DuplicateRegistrationResponseCode = -110;

    public MultiTrialSubmissionService(
        ITrialRegistrationRepository repo,
        IEmailVerificationService emailVerification,
        ISkyriseKeycodeClient skyriseKeycodeClient,
        ILogger<MultiTrialSubmissionService> logger)
    {
        _repo = repo;
        _emailVerification = emailVerification;
        _skyriseKeycodeClient = skyriseKeycodeClient;
        _logger = logger;
    }

    public async Task<ServiceResult<MultiTrialSubmissionResponse>> SubmitAsync(
        MultiTrialSubmissionRequest request,
        CancellationToken ct = default)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
            return ServiceResult<MultiTrialSubmissionResponse>.Invalid(errors);

        var emailVerification = await _emailVerification.VerifyAsync(request.CustomerEmail!, ct);
        if (emailVerification.IsExplicitlyInvalid)
            return ServiceResult<MultiTrialSubmissionResponse>.Invalid(["customer_email is invalid"]);

        var trialRegistrationJson = BuildTrialRegistrationJson(request);

        _logger.LogInformation(
            "Multi Trial submission: license_category_name={Category} license_seats={Seats} trial_days={Days} modules={ModuleCount}",
            request.LicenseCategoryName, request.LicenseSeats, request.TrialDays, request.Modules.Count);

        var result = await _repo.InsertTrialRegistrationAsync(request, trialRegistrationJson, ct);

        if (result is null)
            return ServiceResult<MultiTrialSubmissionResponse>.Error("Trial registration could not be created");

        // The success path returns no response_code column — only duplicate (-110) and error (-200) do.
        if (result.ResponseCode == DuplicateRegistrationResponseCode)
        {
            _logger.LogInformation(
                "Duplicate trial registration detected for license_category_name={Category}",
                request.LicenseCategoryName);

            return ServiceResult<MultiTrialSubmissionResponse>.Invalid(
                [result.Message ?? "Duplicate registration"]);
        }

        if (result.ResponseCode is < 0)
        {
            _logger.LogError(
                "usp_trial_insert_trial_registration failed with response_code={ResponseCode} message={Message}",
                result.ResponseCode, result.Message);

            return ServiceResult<MultiTrialSubmissionResponse>.Error(
                result.Message ?? "Trial registration failed");
        }

        if (result.TrialRegistrationId is null or <= 0)
            return ServiceResult<MultiTrialSubmissionResponse>.Error("Trial registration could not be created");

        var keycodeResult = await _skyriseKeycodeClient.GenerateAsync(
            new KeycodeRequest
            {
                LicenseCategory = result.LicenseCategoryName ?? request.LicenseCategoryName!,
                LicenseDistCode = result.LicenseDistributionMethodCode
                    ?? request.LicenseDistributionMethodCode
                    ?? string.Empty,
                FirstName = request.FirstName ?? string.Empty,
                LastName = request.LastName ?? string.Empty,
                Email = request.CustomerEmail!,
                DurationInDays = request.TrialDays,
                Seats = result.LicenseSeats ?? request.LicenseSeats,
                Iso = request.Country ?? request.LocationCode,
                LicenseType = result.LicenseType,
                LicenseModules = result.LicenseModuleList ?? string.Empty
            },
            ct);

        if (!keycodeResult.Success)
        {
            _logger.LogWarning(
                "Skyrise keycode generation failed for trial registration {TrialRegistrationId}; continuing without a keycode. Error: {ErrorMessage}",
                result.TrialRegistrationId,
                keycodeResult.ErrorMessage);
        }

        return ServiceResult<MultiTrialSubmissionResponse>.Ok(new MultiTrialSubmissionResponse
        {
            TrialRegistrationId = result.TrialRegistrationId.Value,
            Keycode = keycodeResult.Keycode,
            RequestType = result.RequestType,
            FormType = request.FormType,
            LicenseCategoryName = result.LicenseCategoryName ?? request.LicenseCategoryName,
            LicenseCategoryId = result.LicenseCategoryId,
            LicenseSeats = result.LicenseSeats ?? request.LicenseSeats,
            ProductLineId = result.ProductLineId,
            LicenseModuleList = result.LicenseModuleList,
            LicenseType = result.LicenseType,
            LicenseTypeId = result.LicenseTypeId,
            CapabilityActivationDays = result.CapabilityActivationDays,
            StorageGb = result.StorageGb,
            LicenseDistributionMethodCode = result.LicenseDistributionMethodCode,
            Modules = request.Modules
                .Select(m => new MultiTrialModuleResponse
                {
                    LicenseCategoryName = m.LicenseCategoryName!,
                    LicenseSeats = m.LicenseSeats,
                    VaultId = m.VaultId
                })
                .ToList()
        });
    }

    // ── Validation ──────────────────────────────────────────────────────────────

    private static List<string> Validate(MultiTrialSubmissionRequest r)
    {
        var errors = new List<string>();

        if (!string.Equals(r.FormType, MultiFormType, StringComparison.OrdinalIgnoreCase))
            errors.Add($"form_type must be '{MultiFormType}'");

        if (string.IsNullOrWhiteSpace(r.CustomerEmail))
            errors.Add("customer_email is required");
        else if (!IsValidEmail(r.CustomerEmail))
            errors.Add("customer_email must be a valid email address");

        // Enterprise required fields (PartnerCartTrial::ENTERPRISE_REQUIRED_FIELDS)
        if (string.IsNullOrWhiteSpace(r.FirstName))
            errors.Add("first_name is required");

        if (string.IsNullOrWhiteSpace(r.LastName))
            errors.Add("last_name is required");

        if (string.IsNullOrWhiteSpace(r.CompanyName))
            errors.Add("company_name is required");

        if (string.IsNullOrWhiteSpace(r.Country))
            errors.Add("country is required");

        if (r.CompanyTypeId.HasValue && r.CompanyTypeId <= 0)
            errors.Add("company_type_id must be a positive integer if provided");

        if (string.IsNullOrWhiteSpace(r.LicenseCategoryName))
            errors.Add("license_category_name is required");
        else if (string.Equals(r.LicenseCategoryName, PillrBaseProduct, StringComparison.OrdinalIgnoreCase))
            errors.Add($"license_category_name '{PillrBaseProduct}' cannot be trialed");

        if (r.LicenseSeats <= 0)
            errors.Add("license_seats must be a positive integer");

        if (!ValidTrialDays.Contains(r.TrialDays))
            errors.Add($"trial_days must be one of: {string.Join(", ", ValidTrialDays)}");

        if (r.LicenseKeycodeTypeId is not (1 or 3))
            errors.Add("license_keycode_type_id must be 1 or 3");

        if (!string.IsNullOrWhiteSpace(r.PartnerKey) && !Guid.TryParse(r.PartnerKey, out _))
            errors.Add("partner_key must be a valid GUID if provided");

        errors.AddRange(ValidateModules(r));

        return errors;
    }

    private static List<string> ValidateModules(MultiTrialSubmissionRequest r)
    {
        var errors = new List<string>();

        foreach (var (module, index) in r.Modules.Select((m, i) => (m, i)))
        {
            var prefix = $"modules[{index}]";

            if (string.IsNullOrWhiteSpace(module.LicenseCategoryName))
            {
                errors.Add($"{prefix}.license_category_name is required");
            }
            else if (string.Equals(module.LicenseCategoryName, PillrComplianceStorage, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{prefix}.license_category_name '{PillrComplianceStorage}' cannot be trialed");
            }
            else if (!ValidModuleCategories.Contains(module.LicenseCategoryName, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add($"{prefix}.license_category_name must be one of: {string.Join(", ", ValidModuleCategories)}");
            }

            if (module.LicenseSeats < 1)
                errors.Add($"{prefix}.license_seats must be at least 1");
        }

        var categories = r.Modules
            .Where(m => !string.IsNullOrWhiteSpace(m.LicenseCategoryName))
            .Select(m => m.LicenseCategoryName!)
            .ToList();

        var hasPillrSubModule = categories.Any(c => PillrSubModules.Contains(c, StringComparer.OrdinalIgnoreCase));
        var hasPillrParent = categories.Any(c => PillrParentModules.Contains(c, StringComparer.OrdinalIgnoreCase));

        if (hasPillrSubModule && !hasPillrParent)
            errors.Add($"modules {string.Join("/", PillrSubModules)} require a {string.Join(" or ", PillrParentModules)} parent module");

        if ((hasPillrParent || hasPillrSubModule) && r.TrialDays != PillrRequiredTrialDays)
            errors.Add($"trial_days must be {PillrRequiredTrialDays} when a Pillr module is selected");

        return errors;
    }

    private static bool IsValidEmail(string email)
        => System.Net.Mail.MailAddress.TryCreate(email, out _);

    // ── trial_registration_json ─────────────────────────────────────────────────

    /// <summary>
    /// Builds the JSON consumed by usp_trial_insert_trial_registration (OPENJSON).
    /// Mirrors TrialRegistration::buildTrialRegistrationInsert() in the legacy PHP model.
    /// </summary>
    private static string BuildTrialRegistrationJson(MultiTrialSubmissionRequest r)
    {
        var startDate = DateTime.UtcNow.Date;
        var expirationDate = startDate.AddDays(r.TrialDays);

        var licenseCategory = new List<object>
        {
            new
            {
                license_category_name = r.LicenseCategoryName,
                license_seats = r.LicenseSeats,
                days = r.TrialDays,
                category_type_name = TrialCategoryTypeName,
                item_hierarchy_id = PrimaryHierarchyId,
                start_date = Format(startDate),
                expiration_date = Format(expirationDate)
            }
        };

        licenseCategory.AddRange(r.Modules.Select(m => new
        {
            license_category_name = m.LicenseCategoryName,
            license_seats = m.LicenseSeats,
            days = r.TrialDays,
            category_type_name = TrialCategoryTypeName,
            item_hierarchy_id = SecondaryHierarchyId,
            start_date = Format(startDate),
            expiration_date = Format(expirationDate),
            vault_id = m.VaultId
        }));

        return JsonSerializer.Serialize(new
        {
            // The legacy payload always sends an empty keycode here; it is filled in by the
            // downstream trial-creation slice once Skyrise keycode generation is wired in.
            keycode = string.Empty,
            license_keycode_type_id = r.LicenseKeycodeTypeId,
            license_distribution_method_code = r.LicenseDistributionMethodCode ?? string.Empty,
            language_code = r.LanguageCode,
            location_code = r.LocationCode,
            license_category = licenseCategory,
            customer = new
            {
                customer_email = r.CustomerEmail,
                first_name = r.FirstName,
                last_name = r.LastName,
                company_name = r.CompanyName,
                company_type_id = r.CompanyTypeId,
                company_type_name = r.CompanyTypeName,
                phone_number = r.PhoneNumber,
                address_1 = r.Address1,
                address_2 = r.Address2,
                city = r.City,
                state = r.State,
                postal_code = r.PostalCode,
                country = r.Country,
                opt_in = r.OptIn
            },
            partner = new
            {
                partner_key = string.IsNullOrWhiteSpace(r.PartnerKey) ? null : r.PartnerKey,
                account_user_name = r.AccountUserName,
                partner_account_code = r.PartnerAccountCode,
                partner_product_id = r.PartnerProductId
            },
            sfdc = new
            {
                sfdc_lead_id = r.SfdcLeadId,
                sfdc_opportunity_id = r.SfdcOpportunityId,
                sfdc_trial_id = r.SfdcTrialId,
                salesforce_campaign_id = r.SalesforceCampaignId,
                sfdc_distributor_id = r.SfdcDistributorId,
                sfdc_reseller_id = r.SfdcResellerId,
                salesforce_license_id = r.SalesforceLicenseId
            }
        });
    }

    // The stored procedure reads the dates as datetime; the legacy payload uses Y-m-d.
    private static string Format(DateTime date) => date.ToString("yyyy-MM-dd");
}
