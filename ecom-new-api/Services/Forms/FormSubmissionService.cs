using System.Globalization;
using System.Text.Json;
using ecom_new_api.Helpers;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.Forms;
using ecom_new_api.Services.Skyrise;

namespace ecom_new_api.Services.Forms;

/// <summary>
/// Trial-registration form pipeline, ported from the legacy flow:
///
///   1. Persist the raw submission           -> usp_form_insert_form_submit
///   2. Resolve the trial type from form_name (consumer vs business)
///   3. Validate on the trial-type rules      (ConsumerTrialForm / BusinessTrialForm)
///   4. Transform the entity                  (keycode type, default trial days, seats, eloqua)
///   5. Build trial_registration_json         (TrialRegistration::buildTrialRegistrationInsert)
///   6. Insert the trial registration         -> usp_trial_insert_trial_registration
///   7. Generate the Skyrise keycode          -> SkyIdentity token + SkyRise bulk request
///   8. Persist and return the form response  -> usp_form_insert_form_response
/// </summary>
public sealed class FormSubmissionService : IFormSubmissionService
{
    // TrialRegistration::DEFAULT_TRIAL_DAYS
    private static readonly Dictionary<string, int> DefaultTrialDays = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SAEP"] = 30,
        ["SAUP"] = 30,
        ["ADE"] = 30,
        ["WSAV"] = 14,
        ["WSAI"] = 14,
        ["WSAC"] = 14,
        ["WIFI"] = 7
    };

    private const int FallbackTrialDays = 30;

    // ConsumerTrialForm::$categoryConfig — allowed seat values per consumer category.
    private static readonly Dictionary<string, int[]> ConsumerCategorySeats = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WSAV"] = [1],
        ["WSAI"] = [3],
        ["WSAC"] = [5],
        ["WE"] = [1, 3, 5]
    };

    private static readonly string[] ConsumerCategories = ["WSAV", "WSAI", "WSAC", "WE"];
    private static readonly string[] BusinessCategories = ["SAEP", "SAUP", "SAWS", "ADE", "CBEP", "OTSF"];

    // ConsumerTrialForm 'productTrialDays' list.
    private static readonly string[] ConsumerTrialDaysWithoutDistCode = ["14", "30"];
    private static readonly string[] ConsumerTrialDaysWithDistCode = ["45", "60", "90"];

    // BusinessTrialForm company_type_id list.
    private static readonly string[] BusinessCompanyTypeIds = ["14", "1", "3", "2", "11"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    private readonly IFormRepository _repository;
    private readonly ISkyriseKeycodeService _keycodeService;
    private readonly ILogger<FormSubmissionService> _logger;

    public FormSubmissionService(
        IFormRepository repository,
        ISkyriseKeycodeService keycodeService,
        ILogger<FormSubmissionService> logger)
        => (_repository, _keycodeService, _logger) = (repository, keycodeService, logger);

    public async Task<FormSubmissionResult> SubmitAsync(
        FormSubmissionRequest request,
        IDictionary<string, string?> rawFields,
        string? ipAddress,
        CancellationToken ct = default)
    {
        // FormSubmit model: form_name is the only strictly required field to record a submission.
        if (!FormValidation.NotEmpty(request.FormName))
            return FormSubmissionResult.Invalid("form_name", "This field is required");

        var formName = request.FormName!.Trim();
        var trialType = ResolveTrialType(formName, request.FormType);

        if (trialType == TrialType.Unsupported)
            return FormSubmissionResult.Invalid("form_name", "Invalid form name");

        var formJson = JsonSerializer.Serialize(rawFields, JsonOptions);

        long formSubmitId;
        try
        {
            formSubmitId = await _repository.InsertFormSubmitAsync(
                formName, ipAddress, request.FormUrl, formJson, request.CustomerEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record form submission for {FormName}", formName);
            return FormSubmissionResult.Error("Unable to record the form submission.");
        }

        // Locale drives language/location codes when they are not sent explicitly.
        ApplyLocale(request);

        var errors = trialType == TrialType.Consumer
            ? ValidateConsumerTrial(request)
            : ValidateBusinessTrial(request);

        if (errors.Count > 0)
            return FormSubmissionResult.Invalid(errors);

        var entity = Transform(request, trialType);
        var trialJson = BuildTrialRegistrationInsert(entity, rawFields);

        TrialRegistrationInsertOutcome outcome;
        try
        {
            var inserted = await _repository.InsertTrialRegistrationAsync(trialJson);
            outcome = new TrialRegistrationInsertOutcome(
                inserted?.TrialRegistrationId,
                inserted?.Keycode,
                inserted?.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Trial registration insert failed for submission {FormSubmitId}", formSubmitId);
            return FormSubmissionResult.Error("Unable to complete the trial registration.");
        }

        if (!string.IsNullOrWhiteSpace(outcome.ErrorMessage))
        {
            // Legacy behaviour: duplicate trials surface against customer_email so the UI can show them.
            return FormSubmissionResult.Invalid("customer_email", outcome.ErrorMessage!);
        }

        var keycode = await GenerateSkyriseKeycodeAsync(entity, ct) ?? outcome.Keycode;

        var trial = new TrialRegistrationResult
        {
            TrialRegistrationId = outcome.TrialRegistrationId,
            Keycode = keycode,
            LicenseCategoryName = entity.LicenseCategoryName,
            LicenseSeats = entity.LicenseSeats,
            TrialDays = entity.TrialDays,
            StartDate = entity.StartDate,
            ExpirationDate = entity.ExpirationDate,
            CustomerEmail = entity.CustomerEmail
        };

        var responsePayload = BuildEntityPayload(entity, trial, formSubmitId);
        var formResponseJson = JsonSerializer.Serialize(responsePayload, JsonOptions);

        string formResponseKey;
        try
        {
            var formResponse = await _repository.InsertFormResponseAsync(formSubmitId, formResponseJson);
            formResponseKey = formResponse.FormResponseKey ?? $"frm_{Guid.NewGuid():N}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record form response for submission {FormSubmitId}", formSubmitId);
            return FormSubmissionResult.Error("Unable to record the form response.");
        }

        return FormSubmissionResult.Ok(new FormSubmissionResponse
        {
            ResponseCode = 0,
            FormResponseKey = formResponseKey,
            FormSubmitId = formSubmitId,
            Entity = responsePayload
        });
    }

    // ── Trial type resolution ──────────────────────────────────────────────────

    private static TrialType ResolveTrialType(string formName, string? formType)
    {
        if (string.Equals(formType, "GSM", StringComparison.OrdinalIgnoreCase))
            return TrialType.Business;

        if (formName.Contains("consumer", StringComparison.OrdinalIgnoreCase))
            return TrialType.Consumer;

        if (formName.Contains("trialregistration", StringComparison.OrdinalIgnoreCase)
            || formName.Contains("trial", StringComparison.OrdinalIgnoreCase))
            return TrialType.Business;

        return TrialType.Unsupported;
    }

    private static void ApplyLocale(FormSubmissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Locale))
            return;

        var parts = request.Locale.Replace('-', '_').Split('_', StringSplitOptions.RemoveEmptyEntries);

        if (string.IsNullOrWhiteSpace(request.LanguageCode) && parts.Length > 0)
            request.LanguageCode = parts[0].ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(request.LocationCode) && parts.Length > 1)
            request.LocationCode = parts[1].ToUpperInvariant();
    }

    // ── Validation (ConsumerTrialForm::$validates) ─────────────────────────────

    private static Dictionary<string, List<string>> ValidateConsumerTrial(FormSubmissionRequest r)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (!FormValidation.NotEmpty(r.FirstName))
            Add(errors, "first_name", "This field is required");

        if (!FormValidation.NotEmpty(r.LastName))
            Add(errors, "last_name", "This field is required");

        if (!FormValidation.IsEmail(r.CustomerEmail))
            Add(errors, "customer_email", "Invalid email");

        if (!FormValidation.Matches(r.ConfirmCustomerEmail, r.CustomerEmail))
            Add(errors, "confirm_customer_email", "Email does not match");

        if (!FormValidation.IsCountryIso(r.Country))
            Add(errors, "country", "ERR_INVALID_COUNTRY");

        // 'productTrialDays': allowed values depend on whether a distribution code was supplied.
        if (FormValidation.NotEmpty(r.TrialDays))
        {
            var allowed = FormValidation.NotEmpty(r.LicenseDistributionMethodCode)
                ? ConsumerTrialDaysWithDistCode
                : ConsumerTrialDaysWithoutDistCode;

            if (!FormValidation.InList(r.TrialDays, allowed))
                Add(errors, "trial_days", "Invalid number of trial days");
        }

        if (!FormValidation.InList(r.LicenseCategoryName, ConsumerCategories))
        {
            Add(errors, "license_category_name", "Invalid license category name");
        }
        else if (FormValidation.NotEmpty(r.LicenseSeats))
        {
            var seats = FormValidation.ToInt(r.LicenseSeats);
            var allowedSeats = ConsumerCategorySeats[r.LicenseCategoryName!.Trim()];

            if (seats is null || !allowedSeats.Contains(seats.Value))
                Add(errors, "license_seats", "Invalid license seats value");
        }

        return errors;
    }

    // ── Validation (BusinessTrialForm::$validates) ─────────────────────────────

    private static Dictionary<string, List<string>> ValidateBusinessTrial(FormSubmissionRequest r)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (!FormValidation.NotEmpty(r.FirstName))
            Add(errors, "first_name", "ERR_EMPTY_FIRST_NAME");

        if (!FormValidation.NotEmpty(r.LastName))
            Add(errors, "last_name", "ERR_EMPTY_LAST_NAME");

        if (!FormValidation.NotEmpty(r.CompanyName))
            Add(errors, "company_name", "ERR_EMPTY_COMPANY_NAME");

        if (!FormValidation.IsPhone(r.PhoneNumber))
            Add(errors, "phone_number", "ERR_INVALID_PHONE");

        if (!FormValidation.IsEmail(r.CustomerEmail))
            Add(errors, "customer_email", "ERR_INVALID_EMAIL");

        // confirm_customer_email is optional for business trials, but must match when supplied.
        if (FormValidation.NotEmpty(r.ConfirmCustomerEmail)
            && !FormValidation.Matches(r.ConfirmCustomerEmail, r.CustomerEmail))
        {
            Add(errors, "confirm_customer_email", "ERR_EMPTY_CONFIRM_CUSTOMER_EMAIL");
        }

        if (!FormValidation.IsCountryIso(r.Country))
            Add(errors, "country", "ERR_INVALID_COUNTRY");

        if (!FormValidation.NotEmpty(r.LanguageCode))
            Add(errors, "language_code", "ERR_EMPTY_LANGUAGE_CODE");

        if (!FormValidation.NotEmpty(r.LocationCode))
            Add(errors, "location_code", "ERR_EMPTY_LOCATION_CODE");

        if (FormValidation.NotEmpty(r.LicenseSeats)
            && (!FormValidation.IsPositiveInteger(r.LicenseSeats)
                || !FormValidation.InRange(r.LicenseSeats, 0, 1000000)))
        {
            Add(errors, "license_seats", "ERR_INVALID_LICENSE_SEATS");
        }

        if (!FormValidation.InList(r.CompanyTypeId, BusinessCompanyTypeIds))
            Add(errors, "company_type_id", "ERR_INVALID_COMPANY_TYPE");

        // 'GSM' form type is always forced to SAEP downstream, so the posted category is not validated.
        var isGsm = string.Equals(r.FormType, "GSM", StringComparison.OrdinalIgnoreCase);
        if (!isGsm && !FormValidation.InList(r.LicenseCategoryName, BusinessCategories))
            Add(errors, "license_category_name", "ERR_EMPTY_LICENSE_CATEGORY_NAME");

        return errors;
    }

    private static void Add(Dictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var list))
            errors[field] = list = [];

        list.Add(message);
    }

    // ── Transformation (ConsumerTrialForm/BusinessTrialForm::transformEntity) ───

    private static TrialEntity Transform(FormSubmissionRequest r, TrialType trialType)
    {
        var entity = new TrialEntity
        {
            FormName = r.FormName!.Trim(),
            FormType = r.FormType,
            FormUrl = r.FormUrl,
            FirstName = r.FirstName,
            LastName = r.LastName,
            CustomerEmail = r.CustomerEmail,
            CompanyName = r.CompanyName,
            CompanyTypeId = FormValidation.ToInt(r.CompanyTypeId),
            PhoneNumber = r.PhoneNumber,
            Address1 = r.Address1,
            City = r.City,
            State = r.State?.Trim(),
            PostalCode = r.PostalCode,
            Country = r.Country?.Trim(),
            OptIn = FormValidation.ToBool(r.OptIn),
            LanguageCode = r.LanguageCode,
            LocationCode = r.LocationCode,
            LicenseCategoryName = r.LicenseCategoryName?.Trim().ToUpperInvariant(),
            LicenseSeats = FormValidation.ToInt(r.LicenseSeats),
            LicenseDistributionMethodCode = r.LicenseDistributionMethodCode,
            StorageGb = FormValidation.ToInt(r.StorageGb),
            VaultId = FormValidation.ToInt(r.VaultId),
            UsagePricingModelId = FormValidation.ToInt(r.UsagePricingModelId),
            ProductPlatformId = FormValidation.ToInt(r.ProductPlatformId),
            PartnerKey = r.PartnerKey,
            AccountUserName = r.AccountUserName,
            PartnerAccountCode = r.PartnerAccountCode,
            PartnerProductId = FormValidation.ToInt(r.PartnerProductId),
            SfdcLeadId = r.SfdcLeadId,
            SfdcOpportunityId = r.SfdcOpportunityId,
            SfdcTrialId = r.SfdcTrialId,
            SfdcResellerId = r.SfdcResellerId,
            SfdcDistributorId = r.SfdcDistributorId,
            SalesforceCampaignId = r.SalesforceCampaignId,
            SalesforceLicenseId = r.SalesforceLicenseId,
            SendEmail = true,
            // Years is always 0 for trials.
            Years = 0
        };

        entity.TrialDays = FormValidation.ToInt(r.TrialDays) ?? GetDefaultTrialDays(entity.LicenseCategoryName);

        if (trialType == TrialType.Consumer)
        {
            // Consumer trials always issue a standalone (non-parent) keycode.
            entity.LicenseKeycodeTypeId = 1;

            if (entity.LicenseCategoryName is not null
                && ConsumerCategorySeats.TryGetValue(entity.LicenseCategoryName, out var allowedSeats))
            {
                entity.LicenseSeats ??= allowedSeats[0];
            }
        }
        else
        {
            entity.LicenseKeycodeTypeId = FormValidation.ToInt(r.LicenseKeycodeTypeId) ?? 1;
            entity.InsertQueueRecord = true;

            // SAEP business trials are always a parent license.
            if (string.Equals(entity.LicenseCategoryName, "SAEP", StringComparison.OrdinalIgnoreCase))
                entity.LicenseKeycodeTypeId = 3;

            // Japan always issues parent licenses.
            if (string.Equals(entity.LanguageCode, "ja", StringComparison.OrdinalIgnoreCase))
                entity.LicenseKeycodeTypeId = 3;

            // Global Site Manager always issues a GSM (parent SAEP) license.
            if (string.Equals(entity.FormType, "GSM", StringComparison.OrdinalIgnoreCase))
            {
                entity.LicenseKeycodeTypeId = 3;
                entity.LicenseCategoryName = "SAEP";
            }

            // BRNC distribution is a fixed-length trial.
            if (string.Equals(entity.LicenseDistributionMethodCode, "BRNC", StringComparison.OrdinalIgnoreCase))
                entity.TrialDays = 43;

            entity.CampaignId = r.UtmCampaign ?? string.Empty;
            entity.SalesforceCampaignId ??= r.SearchCampaign ?? string.Empty;
            entity.ProductUserLicenseType =
                entity.FormName.Replace("trialregistration", string.Empty, StringComparison.OrdinalIgnoreCase);

            entity.EloquaParameters = new Dictionary<string, object?>
            {
                ["elqFormName"] = "TrialRequest",
                ["elqSiteID"] = "323",
                ["key"] = "65",
                ["C_Licensed_Contact1"] = "Yes",
                ["campaign_id"] = entity.CampaignId,
                ["lead_source"] = r.LeadSource ?? string.Empty,
                ["CompanyType"] = r.CompanyTypeId ?? string.Empty,
                ["utm_medium"] = r.UtmMedium ?? string.Empty,
                ["utm_source"] = r.UtmSource ?? string.Empty,
                ["utm_campaign"] = r.UtmCampaign ?? string.Empty,
                ["utm_term"] = r.UtmTerm ?? string.Empty,
                ["utm_content"] = r.UtmContent ?? string.Empty,
                ["search_campaign"] = r.SearchCampaign ?? string.Empty
            };
        }

        entity.Modules = ParseModules(r.Modules);

        // Trials always start today.
        var start = DateTime.UtcNow.Date;
        entity.StartDate = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        entity.ExpirationDate = start.AddDays(entity.TrialDays)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        return entity;
    }

    private static int GetDefaultTrialDays(string? licenseCategoryName)
        => licenseCategoryName is not null && DefaultTrialDays.TryGetValue(licenseCategoryName, out var days)
            ? days
            : FallbackTrialDays;

    private static List<Dictionary<string, object?>> ParseModules(string? modules)
    {
        if (string.IsNullOrWhiteSpace(modules))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(modules);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var result = new List<Dictionary<string, object?>>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    result.Add(new Dictionary<string, object?>
                    {
                        ["license_category_name"] = element.GetString()
                    });
                    continue;
                }

                if (element.ValueKind != JsonValueKind.Object)
                    continue;

                var module = new Dictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    // Normalise the legacy module key names.
                    var name = property.Name switch
                    {
                        "license_module_code" => "license_category_name",
                        "license_module_seats" => "license_seats",
                        _ => property.Name
                    };

                    module[name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Number => property.Value.GetDecimal(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => property.Value.ToString()
                    };
                }

                result.Add(module);
            }

            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    // ── trial_registration_json (TrialRegistration::buildTrialRegistrationInsert) ─

    private static string BuildTrialRegistrationInsert(
        TrialEntity e, IDictionary<string, string?> rawFields)
    {
        var licenseCategory = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["license_category_name"] = e.LicenseCategoryName,
                ["license_seats"] = e.LicenseSeats,
                ["days"] = e.TrialDays,
                ["category_type_name"] = "trial",
                ["item_hierarchy_id"] = 1,
                ["vault_id"] = e.VaultId,
                ["usage_pricing_model_id"] = e.UsagePricingModelId,
                ["product_platform_id"] = e.ProductPlatformId,
                ["start_date"] = e.StartDate,
                ["expiration_date"] = e.ExpirationDate
            }
        };

        // Modules are always secondary products on the same trial.
        foreach (var module in e.Modules)
        {
            var entry = new Dictionary<string, object?>(module)
            {
                ["days"] = e.TrialDays,
                ["category_type_name"] = "trial",
                ["item_hierarchy_id"] = 2,
                ["start_date"] = e.StartDate,
                ["expiration_date"] = e.ExpirationDate
            };

            entry.TryAdd("license_seats", e.LicenseSeats);
            licenseCategory.Add(entry);
        }

        var payload = new Dictionary<string, object?>
        {
            ["keycode"] = string.Empty,
            ["license_keycode_type_id"] = e.LicenseKeycodeTypeId,
            ["license_distribution_method_code"] = e.LicenseDistributionMethodCode,
            ["language_code"] = e.LanguageCode,
            ["location_code"] = e.LocationCode,
            ["years"] = e.Years,
            ["send_email"] = e.SendEmail,
            ["insert_queue_record"] = e.InsertQueueRecord,
            ["license_category"] = licenseCategory,
            ["customer"] = new Dictionary<string, object?>
            {
                ["first_name"] = e.FirstName,
                ["last_name"] = e.LastName,
                ["customer_email"] = e.CustomerEmail,
                ["company_name"] = e.CompanyName,
                ["company_type_id"] = e.CompanyTypeId,
                ["phone_number"] = e.PhoneNumber,
                ["address_1"] = e.Address1,
                ["city"] = e.City,
                ["state"] = e.State,
                ["postal_code"] = e.PostalCode,
                ["country"] = e.Country,
                ["opt_in"] = e.OptIn
            },
            ["partner"] = new Dictionary<string, object?>
            {
                ["partner_key"] = e.PartnerKey,
                ["account_user_name"] = e.AccountUserName,
                ["partner_account_code"] = e.PartnerAccountCode,
                ["partner_product_id"] = e.PartnerProductId
            },
            ["sfdc"] = new Dictionary<string, object?>
            {
                ["sfdc_lead_id"] = e.SfdcLeadId,
                ["sfdc_opportunity_id"] = e.SfdcOpportunityId,
                ["sfdc_trial_id"] = e.SfdcTrialId,
                ["sfdc_reseller_id"] = e.SfdcResellerId,
                ["sfdc_distributor_id"] = e.SfdcDistributorId,
                ["salesforce_campaign_id"] = e.SalesforceCampaignId,
                ["salesforce_license_id"] = e.SalesforceLicenseId
            },
            ["eloqua_parameters"] = e.EloquaParameters,
            ["form_name"] = e.FormName,
            ["form_url"] = e.FormUrl,
            ["form_json"] = JsonSerializer.Serialize(rawFields, JsonOptions)
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    // ── Skyrise keycode generation (TrialRegistration save filter) ─────────────

    /// <summary>
    /// Mirrors the legacy GenerateKeycode flow. On failure the Skyrise request is
    /// logged to skyrise_license_failure and null is returned so that the
    /// ecom-generated keycode remains the fallback.
    /// </summary>
    private async Task<string?> GenerateSkyriseKeycodeAsync(TrialEntity e, CancellationToken ct)
    {
        var request = new KeycodeGenerationRequest
        {
            LicenseDistCode = e.LicenseDistributionMethodCode,
            LicenseCategory = e.LicenseCategoryName,
            Storage = e.StorageGb ?? 0,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.CustomerEmail,
            DurationInDays = e.TrialDays,
            Seats = e.LicenseSeats ?? 0,
            IsTrial = true,
            LicenseKeycodeTypeId = e.LicenseKeycodeTypeId,
            Iso = e.Country
        };

        KeycodeGenerationResult result;
        try
        {
            result = await _keycodeService.GenerateAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skyrise keycode generation threw for {Category}", e.LicenseCategoryName);
            result = KeycodeGenerationResult.Failed(ex.Message);
        }

        if (result.Success)
            return result.Keycode;

        _logger.LogInformation(
            "Errors detected with Skyrise trial keycode generation: {Error}", result.ErrorMessage);

        try
        {
            // Debug payload intentionally excludes PII, matching the legacy logging.
            var debugRequest = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["license_dist_code"] = request.LicenseDistCode,
                ["license_category"] = request.LicenseCategory,
                ["storage"] = request.Storage,
                ["duration_in_days"] = request.DurationInDays,
                ["seats"] = request.Seats,
                ["is_trial"] = request.IsTrial,
                ["license_keycode_type_id"] = request.LicenseKeycodeTypeId
            }, JsonOptions);

            await _repository.InsertSkyriseFailureAsync(
                debugRequest,
                JsonSerializer.Serialize(new[] { result.ErrorMessage }, JsonOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to record the Skyrise failure log entry.");
        }

        return null;
    }

    // ── Response payload ───────────────────────────────────────────────────────

    private static Dictionary<string, object?> BuildEntityPayload(
        TrialEntity e, TrialRegistrationResult trial, long formSubmitId)
        => new()
        {
            ["form_name"] = e.FormName,
            ["form_submit_id"] = formSubmitId,
            ["first_name"] = e.FirstName,
            ["last_name"] = e.LastName,
            ["customer_email"] = e.CustomerEmail,
            ["company_name"] = e.CompanyName,
            ["country"] = e.Country,
            ["state"] = e.State,
            ["language_code"] = e.LanguageCode,
            ["location_code"] = e.LocationCode,
            ["license_category_name"] = e.LicenseCategoryName,
            ["license_seats"] = e.LicenseSeats,
            ["license_keycode_type_id"] = e.LicenseKeycodeTypeId,
            ["license_distribution_method_code"] = e.LicenseDistributionMethodCode,
            ["trial_days"] = e.TrialDays,
            ["start_date"] = e.StartDate,
            ["expiration_date"] = e.ExpirationDate,
            ["keycode"] = trial.Keycode,
            ["trial_registration_id"] = trial.TrialRegistrationId,
            ["response_code"] = 0
        };

    private enum TrialType { Consumer, Business, Unsupported }

    private sealed record TrialRegistrationInsertOutcome(
        long? TrialRegistrationId, string? Keycode, string? ErrorMessage);

    /// <summary>
    /// Working entity mirroring the legacy TrialRegistration entity after transformation.
    /// </summary>
    private sealed class TrialEntity
    {
        public string FormName { get; set; } = string.Empty;
        public string? FormType { get; set; }
        public string? FormUrl { get; set; }

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CompanyName { get; set; }
        public int? CompanyTypeId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public bool OptIn { get; set; }

        public string? LanguageCode { get; set; }
        public string? LocationCode { get; set; }

        public string? LicenseCategoryName { get; set; }
        public int? LicenseSeats { get; set; }
        public int LicenseKeycodeTypeId { get; set; } = 1;
        public string? LicenseDistributionMethodCode { get; set; }
        public int? StorageGb { get; set; }
        public int? VaultId { get; set; }
        public int? UsagePricingModelId { get; set; }
        public int? ProductPlatformId { get; set; }
        public List<Dictionary<string, object?>> Modules { get; set; } = [];

        public int TrialDays { get; set; }
        public int Years { get; set; }
        public string? StartDate { get; set; }
        public string? ExpirationDate { get; set; }

        public bool SendEmail { get; set; }
        public bool InsertQueueRecord { get; set; }

        public string? PartnerKey { get; set; }
        public string? AccountUserName { get; set; }
        public string? PartnerAccountCode { get; set; }
        public int? PartnerProductId { get; set; }

        public string? SfdcLeadId { get; set; }
        public string? SfdcOpportunityId { get; set; }
        public string? SfdcTrialId { get; set; }
        public string? SfdcResellerId { get; set; }
        public string? SfdcDistributorId { get; set; }
        public string? SalesforceCampaignId { get; set; }
        public string? SalesforceLicenseId { get; set; }

        public string? CampaignId { get; set; }
        public string? ProductUserLicenseType { get; set; }
        public Dictionary<string, object?>? EloquaParameters { get; set; }
    }
}
