using System.Text.Json;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Repositories.Trials;
using ecom_new_api.Services.EmailVerification;
using ecom_new_api.Services;
using ecom_new_api.Services.Skyrise;
using ecom_new_api.Services.MultiTrial;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Services;

public sealed class MultiTrialSubmissionServiceTests
{
    private readonly Mock<ITrialRegistrationRepository> _repoMock = new();
    private readonly Mock<IEmailVerificationService> _emailVerificationMock = new();
    private readonly Mock<ISkyriseKeycodeClient> _skyriseMock = new();

    private MultiTrialSubmissionService CreateSut()
    {
        _emailVerificationMock
            .Setup(v => v.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebbulaVerificationResult.Allowed());
        _skyriseMock
            .Setup(s => s.GenerateAsync(It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(KeycodeResult.Ok("TEST-KEYCODE"));

        return new(
            _repoMock.Object,
            _emailVerificationMock.Object,
            _skyriseMock.Object,
            NullLogger<MultiTrialSubmissionService>.Instance);
    }

    private static MultiTrialSubmissionRequest ValidRequest(
        List<MultiTrialModuleRequest>? modules = null,
        int trialDays = 30) => new()
        {
            FirstName = "John",
            LastName = "Doe",
            CustomerEmail = "john@company.com",
            CompanyName = "Acme Corp",
            CompanyTypeId = 14,
            PhoneNumber = "555-0123",
            Country = "US",
            State = "CO",
            LanguageCode = "en",
            LocationCode = "us",
            LicenseCategoryName = "SAEP",
            LicenseSeats = 100,
            TrialDays = trialDays,
            Modules = modules ??
            [
                new MultiTrialModuleRequest { LicenseCategoryName = "SDNS", LicenseSeats = 100 },
                new MultiTrialModuleRequest { LicenseCategoryName = "SECA", LicenseSeats = 100 }
            ]
        };

    private static TrialRegistrationProcedureResult SuccessRow() => new()
    {
        TrialRegistrationId = 12345,
        RequestType = "create_master",
        LicenseCategoryName = "SAEP",
        LicenseCategoryId = 220,
        LicenseSeats = 100,
        ProductLineId = 300,
        LicenseType = "enterprise,trial",
        LicenseTypeId = 104,
        LicenseDistributionMethodCode = "TRLS"
    };

    [Fact]
    public async Task SubmitAsync_ValidRequest_ReturnsOkWithRegistrationId()
    {
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessRow());

        var result = await CreateSut().SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Equal(12345, result.Data!.TrialRegistrationId);
        Assert.Equal("TEST-KEYCODE", result.Data.Keycode);
        Assert.Equal("create_master", result.Data.RequestType);
        Assert.Equal("MULTI", result.Data.FormType);
        Assert.Equal(2, result.Data.Modules.Count);
    }

    [Fact]
    public async Task SubmitAsync_BuildsTrialRegistrationJsonWithPrimaryAndSecondaryHierarchy()
    {
        string? capturedJson = null;
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<MultiTrialSubmissionRequest, string, CancellationToken>((_, json, _) => capturedJson = json)
            .ReturnsAsync(SuccessRow());

        await CreateSut().SubmitAsync(ValidRequest());

        Assert.NotNull(capturedJson);
        using var doc = JsonDocument.Parse(capturedJson!);
        var root = doc.RootElement;

        Assert.Equal(3, root.GetProperty("license_keycode_type_id").GetInt32());
        Assert.Equal(string.Empty, root.GetProperty("keycode").GetString());

        var categories = root.GetProperty("license_category").EnumerateArray().ToList();
        Assert.Equal(3, categories.Count);

        var primary = categories[0];
        Assert.Equal("SAEP", primary.GetProperty("license_category_name").GetString());
        Assert.Equal(1, primary.GetProperty("item_hierarchy_id").GetInt32());
        Assert.Equal("trial", primary.GetProperty("category_type_name").GetString());
        Assert.Equal(30, primary.GetProperty("days").GetInt32());

        Assert.All(categories.Skip(1), m => Assert.Equal(2, m.GetProperty("item_hierarchy_id").GetInt32()));
        Assert.Equal("SDNS", categories[1].GetProperty("license_category_name").GetString());
        Assert.Equal("SECA", categories[2].GetProperty("license_category_name").GetString());

        Assert.Equal(14, root.GetProperty("customer").GetProperty("company_type_id").GetInt32());
        Assert.Equal("john@company.com", root.GetProperty("customer").GetProperty("customer_email").GetString());
    }

    [Fact]
    public async Task SubmitAsync_MissingRequiredFields_ReturnsValidationError()
    {
        var request = new MultiTrialSubmissionRequest { LicenseSeats = 0 };

        var result = await CreateSut().SubmitAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains("customer_email is required", result.ValidationErrors);
        Assert.Contains("license_category_name is required", result.ValidationErrors);
        Assert.Contains("license_seats must be a positive integer", result.ValidationErrors);
        _repoMock.Verify(r => r.InsertTrialRegistrationAsync(
            It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_InvalidModuleCategory_ReturnsValidationError()
    {
        var request = ValidRequest([new MultiTrialModuleRequest { LicenseCategoryName = "NOPE", LicenseSeats = 1 }]);

        var result = await CreateSut().SubmitAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.StartsWith("modules[0].license_category_name must be one of"));
    }

    [Fact]
    public async Task SubmitAsync_PillrSubModuleWithoutParent_ReturnsValidationError()
    {
        var request = ValidRequest([new MultiTrialModuleRequest { LicenseCategoryName = "PLRM", LicenseSeats = 1 }]);

        var result = await CreateSut().SubmitAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.Contains("parent module"));
    }

    [Fact]
    public async Task SubmitAsync_InvalidTrialDays_ReturnsValidationError()
    {
        var result = await CreateSut().SubmitAsync(ValidRequest(trialDays: 45));

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, e => e.StartsWith("trial_days must be one of"));
    }

    [Fact]
    public async Task SubmitAsync_InvalidPartnerKey_ReturnsValidationErrorWithoutCallingRepository()
    {
        var request = new MultiTrialSubmissionRequest
        {
            FirstName = "John",
            LastName = "Doe",
            CustomerEmail = "john@company.com",
            CompanyName = "Acme Corp",
            Country = "US",
            LicenseCategoryName = "SAEP",
            LicenseSeats = 100,
            PartnerKey = "uuid-partner-key",
            Modules =
            [
                new MultiTrialModuleRequest { LicenseCategoryName = "SDNS", LicenseSeats = 100 },
                new MultiTrialModuleRequest { LicenseCategoryName = "SECA", LicenseSeats = 100 }
            ]
        };

        var result = await CreateSut().SubmitAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains("partner_key must be a valid GUID if provided", result.ValidationErrors);
        _repoMock.Verify(r => r.InsertTrialRegistrationAsync(
            It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_DuplicateRegistration_ReturnsValidationError()
    {
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrialRegistrationProcedureResult
            {
                ResponseCode = -110,
                Message = "Duplicate registration",
                TrialRegistrationId = 999
            });

        var result = await CreateSut().SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains("Duplicate registration", result.ValidationErrors);
    }

    [Fact]
    public async Task SubmitAsync_ProcedureError_ReturnsError()
    {
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrialRegistrationProcedureResult { ResponseCode = -200, Message = "select failed" });

        var result = await CreateSut().SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Error, result.Kind);
        Assert.Equal("select failed", result.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAsync_InvalidWebbulaEmail_ReturnsValidationError()
    {
        var sut = CreateSut();
        _emailVerificationMock
            .Setup(v => v.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebbulaVerificationResult.Invalid());

        var result = await sut.SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains("customer_email is invalid", result.ValidationErrors);
        _repoMock.Verify(r => r.InsertTrialRegistrationAsync(
            It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_SuccessfulSkyriseGeneration_ReturnsKeycode()
    {
        var sut = CreateSut();
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessRow());
        _skyriseMock.Setup(s => s.GenerateAsync(
                It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(KeycodeResult.Ok("SAEP-KEYCODE"));

        var result = await sut.SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Equal("SAEP-KEYCODE", result.Data!.Keycode);
    }

    [Fact]
    public async Task SubmitAsync_SkyriseFailure_ContinuesWithoutKeycode()
    {
        var sut = CreateSut();
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessRow());
        _skyriseMock.Setup(s => s.GenerateAsync(
                It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(KeycodeResult.Failed("Skyrise unavailable"));

        var result = await sut.SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Null(result.Data!.Keycode);
        _skyriseMock.Verify(s => s.GenerateAsync(
            It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_MapsRegistrationAndCustomerDataToSkyriseRequest()
    {
        var sut = CreateSut();
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessRow());

        KeycodeRequest? capturedRequest = null;
        _skyriseMock.Setup(s => s.GenerateAsync(
                It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<KeycodeRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(KeycodeResult.Ok("SAEP-KEYCODE"));

        await sut.SubmitAsync(ValidRequest());

        Assert.NotNull(capturedRequest);
        Assert.Equal("SAEP", capturedRequest!.LicenseCategory);
        Assert.Equal("TRLS", capturedRequest.LicenseDistCode);
        Assert.Equal("John", capturedRequest.FirstName);
        Assert.Equal("Doe", capturedRequest.LastName);
        Assert.Equal("john@company.com", capturedRequest.Email);
        Assert.Equal(30, capturedRequest.DurationInDays);
        Assert.Equal(100, capturedRequest.Seats);
        Assert.Equal("US", capturedRequest.Iso);
        Assert.Equal("enterprise,trial", capturedRequest.LicenseType);
    }

    [Fact]
    public async Task SubmitAsync_DoesNotCallSkyriseWhenRegistrationFails()
    {
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrialRegistrationProcedureResult { ResponseCode = -200, Message = "select failed" });

        var result = await CreateSut().SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Error, result.Kind);
        _skyriseMock.Verify(s => s.GenerateAsync(
            It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SubmitAsync_NoResultSet_ReturnsError()
    {
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrialRegistrationProcedureResult?)null);

        var result = await CreateSut().SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Error, result.Kind);
    }
}
