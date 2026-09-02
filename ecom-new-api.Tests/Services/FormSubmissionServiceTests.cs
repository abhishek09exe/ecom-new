using System.Text.Json;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Repositories.Forms;
using ecom_new_api.Services.Forms;
using ecom_new_api.Services.Skyrise;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Services;

public sealed class FormSubmissionServiceTests
{
    private readonly Mock<IFormRepository> _repository = new();
    private readonly Mock<ISkyriseKeycodeService> _keycodeService = new();
    private string? _capturedTrialJson;

    public FormSubmissionServiceTests()
    {
        // Skyrise generation is off by default in tests; the ecom keycode remains the fallback.
        _keycodeService
            .Setup(s => s.GenerateAsync(It.IsAny<KeycodeGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(KeycodeGenerationResult.Failed("disabled"));
    }

    private FormSubmissionService CreateService()
    {
        _repository.Setup(r => r.InsertFormSubmitAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(101L);

        _repository.Setup(r => r.InsertTrialRegistrationAsync(It.IsAny<string>()))
            .Callback<string>(json => _capturedTrialJson = json)
            .ReturnsAsync(new TrialRegistrationInsertResult
            {
                TrialRegistrationId = 555,
                Keycode = "TRIAL-KEY"
            });

        _repository.Setup(r => r.InsertFormResponseAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync(new FormResponseInsertResult
            {
                FormResponseId = 9,
                FormResponseKey = "frm_key"
            });

        return new FormSubmissionService(
            _repository.Object, _keycodeService.Object, NullLogger<FormSubmissionService>.Instance);
    }

    private static FormSubmissionRequest ValidConsumerRequest() => new()
    {
        FormName = "ConsumerTrialRegistration",
        FirstName = "John",
        LastName = "Doe",
        CustomerEmail = "john@example.com",
        ConfirmCustomerEmail = "john@example.com",
        Country = "US",
        State = "COL",
        TrialDays = "14",
        LicenseCategoryName = "WSAV",
        LicenseSeats = "1"
    };

    private static FormSubmissionRequest ValidBusinessRequest() => new()
    {
        FormName = "BusinessTrialRegistration",
        FirstName = "Jane",
        LastName = "Smith",
        CompanyName = "Acme Inc",
        PhoneNumber = "303-555-1234",
        CustomerEmail = "jane@acme.com",
        Country = "US",
        Locale = "en_US",
        CompanyTypeId = "14",
        LicenseCategoryName = "SAEP",
        LicenseSeats = "25"
    };

    private static Dictionary<string, string?> Raw(FormSubmissionRequest r) => new()
    {
        ["form_name"] = r.FormName,
        ["customer_email"] = r.CustomerEmail
    };

    private JsonElement TrialJson()
        => JsonDocument.Parse(_capturedTrialJson!).RootElement;

    [Fact]
    public async Task Submit_MissingFormName_ReturnsRequiredError()
    {
        var service = CreateService();

        var result = await service.SubmitAsync(
            new FormSubmissionRequest(), new Dictionary<string, string?>(), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("form_name", result.Errors.Keys);
    }

    [Fact]
    public async Task Submit_ValidConsumerTrial_Succeeds()
    {
        var service = CreateService();
        var request = ValidConsumerRequest();

        var result = await service.SubmitAsync(request, Raw(request), "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.Equal("frm_key", result.Data!.FormResponseKey);
        Assert.Equal(101, result.Data.FormSubmitId);
        Assert.Equal("TRIAL-KEY", result.Data.Entity["keycode"]);
    }

    [Fact]
    public async Task Submit_ConsumerTrial_MismatchedConfirmEmail_ReturnsError()
    {
        var service = CreateService();
        var request = ValidConsumerRequest();
        request.ConfirmCustomerEmail = "other@example.com";

        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("confirm_customer_email", result.Errors.Keys);
    }

    [Fact]
    public async Task Submit_ConsumerTrial_InvalidSeatsForCategory_ReturnsError()
    {
        var service = CreateService();
        var request = ValidConsumerRequest();
        request.LicenseSeats = "3"; // WSAV only allows 1 seat

        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("license_seats", result.Errors.Keys);
    }

    [Fact]
    public async Task Submit_ConsumerTrial_InvalidTrialDaysWithoutDistCode_ReturnsError()
    {
        var service = CreateService();
        var request = ValidConsumerRequest();
        request.TrialDays = "90"; // 90 only allowed when a distribution code is supplied

        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("trial_days", result.Errors.Keys);
    }

    [Fact]
    public async Task Submit_ConsumerTrial_InvalidCountry_ReturnsError()
    {
        var service = CreateService();
        var request = ValidConsumerRequest();
        request.Country = "ZZZ";

        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("country", result.Errors.Keys);
    }

    [Fact]
    public async Task Submit_ConsumerTrial_UsesStandaloneKeycodeType()
    {
        var service = CreateService();
        var request = ValidConsumerRequest();

        await service.SubmitAsync(request, Raw(request), null);

        Assert.Equal(1, TrialJson().GetProperty("license_keycode_type_id").GetInt32());
    }

    [Fact]
    public async Task Submit_BusinessTrial_SaepIssuesParentKeycode()
    {
        var service = CreateService();
        var request = ValidBusinessRequest();

        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, TrialJson().GetProperty("license_keycode_type_id").GetInt32());
    }

    [Fact]
    public async Task Submit_BusinessTrial_JapaneseIssuesParentKeycode()
    {
        var service = CreateService();
        var request = ValidBusinessRequest();
        request.LicenseCategoryName = "SAUP";
        request.Locale = "ja_JP";

        await service.SubmitAsync(request, Raw(request), null);

        Assert.Equal(3, TrialJson().GetProperty("license_keycode_type_id").GetInt32());
    }

    [Fact]
    public async Task Submit_BusinessTrial_GsmFormTypeForcesSaepParent()
    {
        var service = CreateService();
        var request = ValidBusinessRequest();
        request.FormType = "GSM";
        request.LicenseCategoryName = "SAUP";

        await service.SubmitAsync(request, Raw(request), null);

        var json = TrialJson();
        Assert.Equal(3, json.GetProperty("license_keycode_type_id").GetInt32());
        Assert.Equal("SAEP",
            json.GetProperty("license_category")[0].GetProperty("license_category_name").GetString());
    }

    [Fact]
    public async Task Submit_BusinessTrial_BrncDistributionForces43Days()
    {
        var service = CreateService();
        var request = ValidBusinessRequest();
        request.LicenseDistributionMethodCode = "BRNC";

        await service.SubmitAsync(request, Raw(request), null);

        Assert.Equal(43,
            TrialJson().GetProperty("license_category")[0].GetProperty("days").GetInt32());
    }

    [Fact]
    public async Task Submit_BusinessTrial_MissingTrialDaysUsesCategoryDefault()
    {
        var service = CreateService();
        var request = ValidBusinessRequest(); // SAEP default is 30

        await service.SubmitAsync(request, Raw(request), null);

        Assert.Equal(30,
            TrialJson().GetProperty("license_category")[0].GetProperty("days").GetInt32());
    }

    [Fact]
    public async Task Submit_BusinessTrial_InvalidCompanyType_ReturnsError()
    {
        var service = CreateService();
        var request = ValidBusinessRequest();
        request.CompanyTypeId = "99";

        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("company_type_id", result.Errors.Keys);
    }

    [Fact]
    public async Task Submit_BusinessTrial_InvalidPhone_ReturnsError()
    {
        var service = CreateService();
        var request = ValidBusinessRequest();
        request.PhoneNumber = "abc";

        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("phone_number", result.Errors.Keys);
    }

    [Fact]
    public async Task Submit_BusinessTrial_LocaleDerivesLanguageAndLocation()
    {
        var service = CreateService();
        var request = ValidBusinessRequest();
        request.Locale = "de_DE";

        await service.SubmitAsync(request, Raw(request), null);

        var json = TrialJson();
        Assert.Equal("de", json.GetProperty("language_code").GetString());
        Assert.Equal("DE", json.GetProperty("location_code").GetString());
    }

    [Fact]
    public async Task Submit_ModulesAreAddedAsSecondaryLicenseCategories()
    {
        var service = CreateService();
        var request = ValidBusinessRequest();
        request.Modules = """[{"license_module_code":"DNSP","license_module_seats":10}]""";

        await service.SubmitAsync(request, Raw(request), null);

        var categories = TrialJson().GetProperty("license_category");
        Assert.Equal(2, categories.GetArrayLength());
        Assert.Equal("DNSP", categories[1].GetProperty("license_category_name").GetString());
        Assert.Equal(2, categories[1].GetProperty("item_hierarchy_id").GetInt32());
    }

    [Fact]
    public async Task Submit_TrialRegistrationError_ReturnsCustomerEmailError()
    {
        _repository.Setup(r => r.InsertFormSubmitAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(101L);

        _repository.Setup(r => r.InsertTrialRegistrationAsync(It.IsAny<string>()))
            .ReturnsAsync(new TrialRegistrationInsertResult
            {
                ErrorMessage = "Trial already exists for this category."
            });

        var service = new FormSubmissionService(
            _repository.Object, _keycodeService.Object, NullLogger<FormSubmissionService>.Instance);

        var request = ValidConsumerRequest();
        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("customer_email", result.Errors.Keys);
    }

    [Fact]
    public async Task Submit_RecordsFormSubmitAndFormResponse()
    {
        var service = CreateService();
        var request = ValidConsumerRequest();

        await service.SubmitAsync(request, Raw(request), "10.0.0.1");

        _repository.Verify(r => r.InsertFormSubmitAsync(
            "ConsumerTrialRegistration", "10.0.0.1", null, It.IsAny<string>(), "john@example.com"), Times.Once);
        _repository.Verify(r => r.InsertFormResponseAsync(101L, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Submit_SkyriseKeycodeGenerated_UsesSkyriseKeycode()
    {
        _keycodeService
            .Setup(s => s.GenerateAsync(It.IsAny<KeycodeGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(KeycodeGenerationResult.Ok("SKY-1234"));

        var service = CreateService();
        var request = ValidConsumerRequest();

        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.True(result.IsSuccess);
        Assert.Equal("SKY-1234", result.Data!.Entity!["keycode"]);
    }

    [Fact]
    public async Task Submit_SkyriseFailure_FallsBackToEcomKeycodeAndLogsFailure()
    {
        var service = CreateService();
        var request = ValidConsumerRequest();

        var result = await service.SubmitAsync(request, Raw(request), null);

        Assert.True(result.IsSuccess);
        Assert.Equal("TRIAL-KEY", result.Data!.Entity!["keycode"]);
        _repository.Verify(
            r => r.InsertSkyriseFailureAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Submit_BusinessSaepTrial_RequestsTemplateKeycode()
    {
        KeycodeGenerationRequest? captured = null;
        _keycodeService
            .Setup(s => s.GenerateAsync(It.IsAny<KeycodeGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<KeycodeGenerationRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(KeycodeGenerationResult.Ok("SKY-TEMPLATE"));

        var service = CreateService();
        var request = ValidBusinessRequest();

        await service.SubmitAsync(request, Raw(request), null);

        Assert.NotNull(captured);
        Assert.Equal("SAEP", captured!.LicenseCategory);
        Assert.Equal(3, captured.LicenseKeycodeTypeId);
        Assert.True(captured.IsTrial);
    }
}
