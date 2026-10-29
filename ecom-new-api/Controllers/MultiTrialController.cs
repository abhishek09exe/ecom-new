using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ecom_new_api.Helpers;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.Forms;
using ecom_new_api.Repositories.LicenseOptions;
using ecom_new_api.Services;
using ecom_new_api.Services.MultiTrial;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ecom_new_api.Controllers;

/// <summary>
/// Multi Trial (GSM net-new trial) submissions.
///
/// Legacy equivalent: POST /forms/submissions with
/// form_name = "BusinessTrialRegistration" and form_type = "MULTI".
/// </summary>
[Route("multi-trial")]
public sealed class MultiTrialController : ControllerBase
{
    private const string MultiTrialFormName = "BusinessTrialRegistration";
    private static readonly Regex FormModuleFieldPattern = new(
        @"^modules\[(?<index>\d+)\]\[(?<field>license_category_name|license_seats)\]$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private readonly IMultiTrialSubmissionService _service;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly ILicenseOptionsRepository _licenseOptionsRepository;
    private readonly ILogger<MultiTrialController> _logger;
    private readonly IMultiTrialNotificationService? _notifications;
    private static readonly JsonSerializerOptions LegacyFormJsonOptions = new()
    {
        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
    };

    public MultiTrialController(
        IMultiTrialSubmissionService service,
        IFormResponseRepository formResponseRepository,
        ILicenseOptionsRepository licenseOptionsRepository,
        ILogger<MultiTrialController> logger,
        IMultiTrialNotificationService? notifications = null)
    {
        _service = service;
        _formResponseRepository = formResponseRepository;
        _licenseOptionsRepository = licenseOptionsRepository;
        _logger = logger;
        _notifications = notifications;
    }

    /// <summary>
    /// Creates the trial registration for a Multi Trial submission
    /// (primary product plus the selected secondary modules).
    /// </summary>
    [HttpPost("~/forms/submissions")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<MultiTrialSubmissionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    public Task<IActionResult> CreateSubmission(
        [FromBody] MultiTrialSubmissionRequest request,
        CancellationToken ct)
        => SubmitRequestAsync(request, ct);

    [HttpPost("~/forms/submissions")]
    [Consumes("application/x-www-form-urlencoded")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(ApiResponse<MultiTrialSubmissionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateFormSubmission(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        var request = BindFormRequest(form);
        return await SubmitRequestAsync(request, ct,
            response => CreateLegacyFormResponseAsync(request, form, response, ct));
    }

    private async Task<IActionResult> SubmitRequestAsync(
        MultiTrialSubmissionRequest? request,
        CancellationToken ct,
        Func<MultiTrialSubmissionResponse, Task<IActionResult>>? successResponse = null)
    {
        if (!ModelState.IsValid)
            return UnprocessableEntity(new { errors = GetModelBindingErrors() });

        if (request is null)
            return UnprocessableEntity(new { errors = SingleFieldError("request", "request body is required") });

        var formErrors = new List<string>();
        if (!string.Equals(request.FormName, MultiTrialFormName, StringComparison.OrdinalIgnoreCase))
            formErrors.Add($"form_name must be '{MultiTrialFormName}'");
        if (!string.Equals(request.FormType, "MULTI", StringComparison.OrdinalIgnoreCase))
            formErrors.Add("form_type must be 'MULTI'");
        if (formErrors.Count > 0)
            return UnprocessableEntity(new { errors = ToFieldErrors(formErrors) });

        _logger.LogInformation(
            "Multi Trial submission request: form_type={FormType} license_category_name={Category} modules={ModuleCount}",
            request.FormType, request.LicenseCategoryName, request.Modules.Count);

        var result = await _service.SubmitAsync(request, ct);

        if (result.Kind == ServiceResultKind.Ok)
        {
            if (successResponse is not null)
                return await successResponse(result.Data!);

            if (_notifications is not null)
                await _notifications.CompleteAsync(request, result.Data!,
                    JsonSerializer.Serialize(request, LegacyFormJsonOptions),
                    HttpContext.Connection.RemoteIpAddress?.ToString(), Request.Headers.Referer.ToString(), null, ct);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponse<MultiTrialSubmissionResponse>.Success(result.Data!));
        }

        return result.Kind switch
        {
            ServiceResultKind.ValidationError =>
                UnprocessableEntity(new { errors = ToFieldErrors(result.ValidationErrors) }),

            ServiceResultKind.NotFound =>
                NotFound(ApiResponse<MultiTrialSubmissionResponse>.Failure(result.ErrorMessage ?? "Not found")),

            _ => StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<MultiTrialSubmissionResponse>.Failure(result.ErrorMessage ?? "An unexpected error occurred"))
        };
    }

    private async Task<IActionResult> CreateLegacyFormResponseAsync(
        MultiTrialSubmissionRequest request,
        IFormCollection form,
        MultiTrialSubmissionResponse result,
        CancellationToken ct)
    {
        if (result.TrialId <= 0 || string.IsNullOrWhiteSpace(result.Keycode))
            return LegacyResponseFailure("The created trial is missing required response data.");

        try
        {
            var license = await _licenseOptionsRepository.SelectLicenseOptionsAsync(result.Keycode, ct: ct);
            if (license?.LicenseId is null || string.IsNullOrWhiteSpace(license.LicenseKey))
                return LegacyResponseFailure("The created trial license key could not be retrieved.");

            var formJson = BuildLegacyFormJson(form);
            var formSubmitId = await _formResponseRepository.InsertFormSubmitAsync(
                MultiTrialFormName,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.Referer.ToString(),
                formJson,
                ct);
            if (formSubmitId is null or <= 0)
                return LegacyResponseFailure("The form submission record could not be saved.");

            var entity = new LegacyMultiTrialFormEntity
            {
                FormSubmitId = formSubmitId.Value,
                FormName = request.FormName,
                FormType = request.FormType,
                CustomerEmail = request.CustomerEmail,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CompanyName = request.CompanyName,
                CompanyTypeId = request.CompanyTypeId,
                PhoneNumber = request.PhoneNumber,
                Country = request.Country,
                State = request.State,
                LocationCode = request.LocationCode,
                LicenseId = license.LicenseId,
                TrialRegistrationId = result.TrialRegistrationId,
                Keycode = result.Keycode,
                LicenseCategoryName = result.LicenseCategoryName ?? request.LicenseCategoryName,
                LicenseCategoryId = result.LicenseCategoryId,
                LicenseSeats = result.LicenseSeats ?? request.LicenseSeats,
                TrialDays = request.TrialDays,
                LicenseKeycodeTypeId = request.LicenseKeycodeTypeId,
                LicenseDistributionMethodCode = result.LicenseDistributionMethodCode,
                LicenseType = result.LicenseType,
                LicenseTypeId = result.LicenseTypeId,
                CapabilityActivationDays = result.CapabilityActivationDays,
                FormJson = formJson,
                CampaignId = GetFirstFormValue(form, "utm_campaign"),
                SalesforceCampaignId = GetFirstFormValue(form, "search_campaign"),
                Trial = new LegacyMultiTrialFormTrial
                {
                    TrialId = result.TrialId,
                    TrialRegistrationId = result.TrialRegistrationId,
                    LicenseKey = license.LicenseKey,
                    Keycode = result.Keycode
                },
                Modules = result.Modules
            };

            var snapshot = new LegacyFormSubmissionSnapshot
            {
                FormSubmitId = formSubmitId.Value,
                FormName = MultiTrialFormName,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                FormUrl = Request.Headers.Referer.ToString(),
                FormJson = formJson,
                Entity = entity
            };
            var responseKey = await _formResponseRepository.InsertFormResponseAsync(
                formSubmitId.Value,
                JsonSerializer.Serialize(snapshot, LegacyFormJsonOptions),
                ct);
            if (string.IsNullOrWhiteSpace(responseKey))
                return LegacyResponseFailure("The form response key was not returned by persistence.");

            if (_notifications is not null)
                await _notifications.CompleteAsync(request, result, formJson, snapshot.IpAddress,
                    snapshot.FormUrl, formSubmitId.Value, ct);

            return StatusCode(StatusCodes.Status201Created, new LegacyFormSubmissionResponse
            {
                FormSubmitId = snapshot.FormSubmitId,
                FormName = snapshot.FormName,
                IpAddress = snapshot.IpAddress,
                FormUrl = snapshot.FormUrl,
                FormJson = snapshot.FormJson,
                Entity = snapshot.Entity,
                FormResponseKey = responseKey
            });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Legacy Multi Trial response persistence failed. ExceptionType={ExceptionType}",
                ex.GetType().Name);
            return LegacyResponseFailure("The legacy form response could not be persisted.");
        }
    }

    private static string BuildLegacyFormJson(IFormCollection form)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        var modules = new SortedDictionary<int, Dictionary<string, object? >>();
        var modulePattern = new Regex(
            @"^modules\[(?<index>\d+)\]\[(?<field>[^\]]+)\]$",
            RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        foreach (var key in form.Keys)
        {
            var formValues = form[key];
            object? value = formValues.Count switch
            {
                0 => string.Empty,
                1 => formValues[0],
                _ => formValues.ToArray()
            };

            var match = modulePattern.Match(key);
            if (match.Success && int.TryParse(match.Groups["index"].Value,
                    NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                if (!modules.TryGetValue(index, out var module))
                {
                    module = new Dictionary<string, object?>(StringComparer.Ordinal);
                    modules.Add(index, module);
                }
                module[match.Groups["field"].Value] = value;
            }
            else
            {
                values[key] = value;
            }
        }

        if (modules.Count > 0)
            values["modules"] = modules.Values.ToList();

        return JsonSerializer.Serialize(values, LegacyFormJsonOptions);
    }

    private ObjectResult LegacyResponseFailure(string message)
        => StatusCode(StatusCodes.Status500InternalServerError,
            new { errors = SingleFieldError("request", message) });

    private MultiTrialSubmissionRequest BindFormRequest(IFormCollection form)
    {
        var defaults = new MultiTrialSubmissionRequest();
        var modules = new SortedDictionary<int, FormModuleValues>();

        foreach (var key in form.Keys)
        {
            var match = FormModuleFieldPattern.Match(key);
            if (!match.Success || !int.TryParse(match.Groups["index"].Value, NumberStyles.None,
                    CultureInfo.InvariantCulture, out var index))
                continue;

            var field = match.Groups["field"].Value;
            var value = GetFirstFormValue(form, key);
            if (!modules.TryGetValue(index, out var module))
            {
                module = new FormModuleValues();
                modules.Add(index, module);
            }

            switch (field.ToLowerInvariant())
            {
                case "license_category_name":
                    module.LicenseCategoryName = value;
                    break;
                case "license_seats":
                    module.LicenseSeats = ParseFormInteger(key, value, 0);
                    break;
            }
        }

        return new MultiTrialSubmissionRequest
        {
            FirstName = GetFirstFormValue(form, "first_name"),
            LastName = GetFirstFormValue(form, "last_name"),
            CustomerEmail = GetFirstFormValue(form, "customer_email"),
            CompanyName = GetFirstFormValue(form, "company_name"),
            CompanyTypeId = ParseOptionalFormInteger(form, "company_type_id"),
            PhoneNumber = GetFirstFormValue(form, "phone_number"),
            State = GetFirstFormValue(form, "state"),
            Country = GetFirstFormValue(form, "country"),
            LanguageCode = GetFirstFormValue(form, "language_code") ?? defaults.LanguageCode,
            LocationCode = GetFirstFormValue(form, "location_code") ?? defaults.LocationCode,
            LicenseCategoryName = GetFirstFormValue(form, "license_category_name"),
            LicenseSeats = ParseFormInteger(form, "license_seats", 0),
            TrialDays = defaults.TrialDays,
            LicenseKeycodeTypeId = defaults.LicenseKeycodeTypeId,
            Modules = modules.Values.Select(module => new MultiTrialModuleRequest
            {
                LicenseCategoryName = module.LicenseCategoryName,
                LicenseSeats = module.LicenseSeats
            }).ToList(),
            FormName = GetFirstFormValue(form, "form_name") ?? defaults.FormName,
            FormType = GetFirstFormValue(form, "form_type") ?? defaults.FormType
        };
    }

    private int ParseFormInteger(IFormCollection form, string field, int fallback, bool checkboxOnIsTrue = false)
        => ParseFormInteger(field, GetFirstFormValue(form, field), fallback, checkboxOnIsTrue);

    private int? ParseOptionalFormInteger(IFormCollection form, string field, bool checkboxOnIsTrue = false)
        => ParseOptionalFormInteger(field, GetFirstFormValue(form, field), checkboxOnIsTrue);

    private int ParseFormInteger(string field, string? value, int fallback, bool checkboxOnIsTrue = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        if (checkboxOnIsTrue && string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        ModelState.AddModelError(field, "The request value is invalid.");
        return fallback;
    }

    private int? ParseOptionalFormInteger(string field, string? value, bool checkboxOnIsTrue = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (checkboxOnIsTrue && string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
            return 1;
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        ModelState.AddModelError(field, "The request value is invalid.");
        return null;
    }

    private static string? GetFirstFormValue(IFormCollection form, string field)
        => form.TryGetValue(field, out var values) && values.Count > 0 ? values[0] : null;

    private sealed class FormModuleValues
    {
        public string? LicenseCategoryName { get; set; }
        public int LicenseSeats { get; set; }
    }

    private Dictionary<string, List<string>> GetModelBindingErrors()
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var entry in ModelState)
        {
            if (entry.Value?.Errors.Count is not > 0)
                continue;

            var field = NormalizeFieldName(entry.Key);
            if (!errors.TryGetValue(field, out var fieldErrors))
            {
                fieldErrors = [];
                errors[field] = fieldErrors;
            }

            fieldErrors.AddRange(entry.Value.Errors.Select(error =>
                string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "The request value is invalid."
                    : error.ErrorMessage));
        }

        return errors;
    }

    private static Dictionary<string, List<string>> ToFieldErrors(IEnumerable<string> validationErrors)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var validationError in validationErrors)
        {
            var field = GetValidationField(validationError);
            if (!errors.TryGetValue(field, out var fieldErrors))
            {
                fieldErrors = [];
                errors[field] = fieldErrors;
            }

            fieldErrors.Add(validationError);
        }

        return errors;
    }

    private static string GetValidationField(string validationError)
    {
        const string modulePrefix = "modules[";
        if (validationError.StartsWith(modulePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var separator = validationError.IndexOf(' ');
            if (separator > 0)
                return NormalizeFieldName(validationError[..separator]);
        }
        else if (validationError.StartsWith("modules ", StringComparison.OrdinalIgnoreCase))
        {
            return "modules";
        }

        string[] requestFields =
        [
            "form_name", "form_type", "customer_email", "first_name", "last_name", "company_name",
            "country", "company_type_id", "license_category_name", "license_seats", "trial_days",
            "license_keycode_type_id", "partner_key"
        ];

        foreach (var requestField in requestFields)
        {
            if (validationError.StartsWith(requestField + " ", StringComparison.OrdinalIgnoreCase))
                return requestField;
        }

        return "request";
    }

    private static Dictionary<string, List<string>> SingleFieldError(string field, string message)
        => new(StringComparer.Ordinal) { [field] = [message] };

    private static string NormalizeFieldName(string field)
    {
        if (field.StartsWith("$.", StringComparison.Ordinal))
            field = field[2..];

        var moduleIndexEnd = field.IndexOf(']');
        if (field.StartsWith("modules[", StringComparison.OrdinalIgnoreCase) &&
            moduleIndexEnd > 0 && moduleIndexEnd + 1 < field.Length && field[moduleIndexEnd + 1] == '.')
        {
            var moduleName = field[(moduleIndexEnd + 2)..];
            return "modules" + field[7..(moduleIndexEnd + 1)] + "[" + SnakeCaseNamingPolicy.Instance.ConvertName(moduleName) + "]";
        }

        return SnakeCaseNamingPolicy.Instance.ConvertName(field);
    }
}
