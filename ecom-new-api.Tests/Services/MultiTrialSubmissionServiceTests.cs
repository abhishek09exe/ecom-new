using System.Text.Json;
using ecom_new_api.Data.Entities;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
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
        _repoMock
            .Setup(r => r.InsertTrialAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TrialInsertProcedureResult { TrialId = 30000, FinalKeycode = "TRIAL-KEYCODE" });

        return new(
            _repoMock.Object,
            _emailVerificationMock.Object,
            _skyriseMock.Object,
            NullLogger<MultiTrialSubmissionService>.Instance);
    }

    private static MultiTrialSubmissionRequest ValidRequest(
        List<MultiTrialModuleRequest>? modules = null,
        int trialDays = 30,
        string licenseCategoryName = "SAEP",
        int licenseKeycodeTypeId = 3) => new()
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
            LicenseCategoryName = licenseCategoryName,
            LicenseSeats = 100,
            TrialDays = trialDays,
            LicenseKeycodeTypeId = licenseKeycodeTypeId,
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
    public async Task Notifications_DisabledDoesNotReadOrWritePersistence()
    {
        var licenses = new Mock<ecom_new_api.Repositories.LicenseOptions.ILicenseOptionsRepository>(MockBehavior.Strict);
        var forms = new Mock<ecom_new_api.Repositories.Forms.IFormResponseRepository>(MockBehavior.Strict);
        var notifications = new Mock<IMultiTrialNotificationRepository>(MockBehavior.Strict);
        var service = new MultiTrialNotificationService(
            Microsoft.Extensions.Options.Options.Create(new MultiTrialNotificationOptions()),
            licenses.Object, forms.Object, notifications.Object,
            NullLogger<MultiTrialNotificationService>.Instance);

        await service.CompleteAsync(ValidRequest(), new MultiTrialSubmissionResponse(), "{}", null, null, null, default);
        licenses.VerifyNoOtherCalls();
        forms.VerifyNoOtherCalls();
        notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Notifications_OptionsValidationFailureDoesNotAccessPersistence()
    {
        var options = new Mock<Microsoft.Extensions.Options.IOptions<MultiTrialNotificationOptions>>();
        options.SetupGet(value => value.Value).Throws(new Microsoft.Extensions.Options.OptionsValidationException(
            "MultiTrialNotifications", typeof(MultiTrialNotificationOptions), ["Local validation failure"]));
        var licenses = new Mock<ecom_new_api.Repositories.LicenseOptions.ILicenseOptionsRepository>(MockBehavior.Strict);
        var forms = new Mock<ecom_new_api.Repositories.Forms.IFormResponseRepository>(MockBehavior.Strict);
        var notifications = new Mock<IMultiTrialNotificationRepository>(MockBehavior.Strict);
        var service = new MultiTrialNotificationService(options.Object, licenses.Object, forms.Object,
            notifications.Object, NullLogger<MultiTrialNotificationService>.Instance);

        await service.CompleteAsync(ValidRequest(), new MultiTrialSubmissionResponse { TrialId = 7 },
            "{}", null, null, null, default);
        options.VerifyGet(value => value.Value, Times.Once);
        licenses.VerifyNoOtherCalls();
        forms.VerifyNoOtherCalls();
        notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Notifications_MarketingUsesExistingFormAndTrialWithoutCreatingAnotherForm()
    {
        var licenses = new Mock<ecom_new_api.Repositories.LicenseOptions.ILicenseOptionsRepository>(MockBehavior.Strict);
        var forms = new Mock<ecom_new_api.Repositories.Forms.IFormResponseRepository>(MockBehavior.Strict);
        var notifications = new Mock<IMultiTrialNotificationRepository>();
        var service = new MultiTrialNotificationService(
            Microsoft.Extensions.Options.Options.Create(new MultiTrialNotificationOptions { MarketingEnabled = true }),
            licenses.Object, forms.Object, notifications.Object,
            NullLogger<MultiTrialNotificationService>.Instance);

        await service.CompleteAsync(ValidRequest(), new MultiTrialSubmissionResponse { TrialId = 30000 },
            "{}", null, null, 42, default);
        notifications.Verify(repository => repository.QueueMarketingAsync(42, 30000, default), Times.Once);
        forms.VerifyNoOtherCalls();
        licenses.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Notifications_EmailFailureStillQueuesMarketingWithPreservedForm()
    {
        var licenses = new Mock<ecom_new_api.Repositories.LicenseOptions.ILicenseOptionsRepository>();
        licenses.Setup(repository => repository.SelectLicenseOptionsAsync(It.IsAny<string>(), null, default))
            .ThrowsAsync(new InvalidOperationException("Local test failure"));
        var forms = new Mock<ecom_new_api.Repositories.Forms.IFormResponseRepository>();
        forms.Setup(repository => repository.InsertFormSubmitAsync("BusinessTrialRegistration", null, null,
                "{\"utm_campaign\":\"local-test\"}", default)).ReturnsAsync(42);
        var notifications = new Mock<IMultiTrialNotificationRepository>();
        var service = new MultiTrialNotificationService(
            Microsoft.Extensions.Options.Options.Create(new MultiTrialNotificationOptions
                { EmailEnabled = true, MarketingEnabled = true }),
            licenses.Object, forms.Object, notifications.Object,
            NullLogger<MultiTrialNotificationService>.Instance);

        await service.CompleteAsync(ValidRequest(), new MultiTrialSubmissionResponse
            { TrialId = 30000, Keycode = "LOCAL-TEST" }, "{\"utm_campaign\":\"local-test\"}", null, null, null, default);
        notifications.Verify(repository => repository.QueueMarketingAsync(42, 30000, default), Times.Once);
        notifications.Verify(repository => repository.QueueEmailsAsync(It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<IReadOnlyList<string>>(), default), Times.Never);
    }

    [Fact]
    public async Task Notifications_UsesPersistedProfileForAllConfirmationTemplates()
    {
        var licenses = new Mock<ecom_new_api.Repositories.LicenseOptions.ILicenseOptionsRepository>();
        licenses.Setup(repository => repository.SelectLicenseOptionsAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(new LicenseOptionsResponse
            {
                LicenseId = 7, LicenseKey = "11111111-1111-1111-1111-111111111111",
                LicenseProfile = new Dictionary<string, LicenseProfileEntryResponse>
                {
                    ["SAEP"] = new() { LicenseCategoryName = "SAEP", ItemHierarchyId = 1 },
                    ["SDNS"] = new() { LicenseCategoryName = "SDNS", ItemHierarchyId = 2 },
                    ["SECA"] = new() { LicenseCategoryName = "SECA", ItemHierarchyId = 2 }
                }
            });
        var forms = new Mock<ecom_new_api.Repositories.Forms.IFormResponseRepository>(MockBehavior.Strict);
        var notifications = new Mock<IMultiTrialNotificationRepository>();
        IReadOnlyList<string>? rendered = null;
        notifications.Setup(repository => repository.QueueEmailsAsync(7, "john@company.com",
                It.IsAny<IReadOnlyList<string>>(), default))
            .Callback<int, string, IReadOnlyList<string>, CancellationToken>((_, _, bodies, _) => rendered = bodies)
            .Returns(Task.CompletedTask);
        var service = new MultiTrialNotificationService(
            Microsoft.Extensions.Options.Options.Create(new MultiTrialNotificationOptions { EmailEnabled = true }),
            licenses.Object, forms.Object, notifications.Object,
            NullLogger<MultiTrialNotificationService>.Instance);

        await service.CompleteAsync(ValidRequest(modules: []), new MultiTrialSubmissionResponse
            { TrialId = 30000, Keycode = "LOCAL-TEST", LicenseSeats = 5 }, "{}", null, null, null, default);
        Assert.NotNull(rendered);
        Assert.Equal(3, rendered!.Count);
        Assert.Contains("Business Endpoint Protection", rendered[0]);
        Assert.Contains("DNS Protection Trial Instructions", rendered[1]);
        Assert.Contains("Security Awareness Training Trial Instructions", rendered[2]);
        Assert.Contains("license_seats]=5", rendered[1]);
        Assert.All(rendered, body => Assert.DoesNotContain("{{", body));
        forms.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Notifications_MarketingFailureDoesNotFailTheCreatedTrial()
    {
        var notifications = new Mock<IMultiTrialNotificationRepository>();
        notifications.Setup(repository => repository.QueueMarketingAsync(42, 7, default))
            .ThrowsAsync(new InvalidOperationException("Local queue failure"));
        var service = new MultiTrialNotificationService(
            Microsoft.Extensions.Options.Options.Create(new MultiTrialNotificationOptions { MarketingEnabled = true }),
            Mock.Of<ecom_new_api.Repositories.LicenseOptions.ILicenseOptionsRepository>(),
            Mock.Of<ecom_new_api.Repositories.Forms.IFormResponseRepository>(), notifications.Object,
            NullLogger<MultiTrialNotificationService>.Instance);
        await service.CompleteAsync(ValidRequest(), new MultiTrialSubmissionResponse { TrialId = 7 },
            "{}", null, null, 42, default);
        notifications.Verify(repository => repository.QueueMarketingAsync(42, 7, default), Times.Once);
    }

    [Fact]
    public void ConfirmationRenderer_HtmlEncodesCustomerValuesAndRetainsLegacyLinks()
    {
        var request = new MultiTrialSubmissionRequest { FirstName = "<script>", LastName = "&", CustomerEmail = "test@example.invalid" };
        var bodies = MultiTrialEmailRenderer.Render(request, "LOCAL&TEST", Guid.Empty, 5, ["SAEP", "SDNS"]);
        Assert.Contains("&lt;script&gt;", bodies[0]);
        Assert.DoesNotContain("<script>", bodies[0]);
        Assert.Contains("LOCAL&amp;TEST", bodies[0]);
        Assert.Contains("admingettingstartedguide", bodies[0]);
        Assert.Contains("items[0][license_category_name]=SDNS", bodies[1]);
    }

    [Fact]
    public void ConfirmationRenderer_SaepGsmMatchesLegacyEnglishContentAndHeader()
    {
        var bodies = MultiTrialEmailRenderer.Render(ValidRequest(modules: []), "FAKE-KEYCODE", Guid.Empty, 5, ["SAEP"]);
        var body = Assert.Single(bodies);

        Assert.Contains("Your free trial instructions for Webroot", body);
        Assert.Contains("Dear John Doe", body);
        Assert.Contains("Your trial is registered to: john@company.com", body);
        Assert.Contains("Your keycode is: FAKE-KEYCODE", body);
        Assert.Contains("admingettingstartedguide", body);
        Assert.Contains("https://www.webroot.com/us/en/?utm_source=webroot", body);
        Assert.Contains("image.service-webroot.com", body);
        Assert.Contains("/us/en/legal/privacy", body);
        Assert.DoesNotContain("{{", body);
    }

    [Theory]
    [InlineData("SDNS", "DNS Protection Trial Instructions", "DNS Protection", "DNS Protection Getting Started Guide</a>.", "dnsprotection_gettingstartedguide", "items[0][license_category_name]=SDNS")]
    [InlineData("SECA", "Security Awareness Training Trial Instructions", "Security Awareness Training", "Security Awareness Training Getting Started Guide.</a>", "SecurityAwarenessTraining_GettingStartedGuide.pdf", "items[0][license_category_name]=SECA")]
    public void ConfirmationRenderer_ModuleTemplatesMatchLegacyEnglishProductLinksAndContent(
        string category, string heading, string product, string guideMarkup, string guideUrl, string purchaseCategory)
    {
        var bodies = MultiTrialEmailRenderer.Render(ValidRequest(modules: []), "FAKE-KEYCODE", Guid.Empty, 5,
            ["SAEP", category]);
        var body = Assert.Single(bodies.Skip(1));

        Assert.Contains(heading, body);
        Assert.Contains("Dear Customer", body);
        Assert.Contains($"Thank you for choosing to try out Webroot<sup>&reg;</sup> {product}", body);
        Assert.Contains("Beta Agreement", body);
        Assert.Contains(guideMarkup, body);
        Assert.Contains(guideUrl, body);
        Assert.Contains("items[0][message_key]=", body);
        Assert.Contains(purchaseCategory, body);
        Assert.Contains("items[0][item_hierarchy_id]=2", body);
        Assert.Contains("amer-smb-onln-endpoint-receipt", body);
        Assert.Contains("amer-smb-onln-trial-instructions", body);
        Assert.Contains("amer-smb-onln-receipt", body);
        Assert.Contains("Webroot-opentext-logogreen-250x50.png", body);
        Assert.Contains("utm_campaign=amer-csmr-smb-receipts", body);
        Assert.DoesNotContain("{{", body);
    }

    [Fact]
    public void ConfirmationRenderer_SelectsOneBodyPerSupportedProfileAndRejectsUnknownProfiles()
    {
        var bodies = MultiTrialEmailRenderer.Render(ValidRequest(modules: []), "FAKE-KEYCODE", Guid.Empty, 5,
            ["SAEP", "SDNS", "SECA"]);
        Assert.Equal(3, bodies.Count);
        Assert.Contains("Business Endpoint Protection", bodies[0]);
        Assert.Contains("DNS Protection Trial Instructions", bodies[1]);
        Assert.Contains("Security Awareness Training Trial Instructions", bodies[2]);
        Assert.All(bodies, body => Assert.DoesNotContain("{{", body));
        Assert.Throws<InvalidOperationException>(() => MultiTrialEmailRenderer.Render(
            ValidRequest(modules: []), "FAKE-KEYCODE", Guid.Empty, 5, ["SAEP", "UNSUPPORTED"]));
    }

    [Fact]
    public void JsonRequest_PreservesMarketingFieldsWithoutTreatingEmailAliasAsCustomerEmail()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = ecom_new_api.Helpers.SnakeCaseNamingPolicy.Instance };
        var request = JsonSerializer.Deserialize<MultiTrialSubmissionRequest>(
            "{\"customer_email\":\"test@example.invalid\",\"utm_campaign\":\"local-test\",\"email\":\"ignored@example.invalid\"}", options)!;
        using var snapshot = JsonDocument.Parse(JsonSerializer.Serialize(request, options));
        Assert.Equal("test@example.invalid", request.CustomerEmail);
        Assert.Equal("local-test", snapshot.RootElement.GetProperty("utm_campaign").GetString());
    }

    [Fact]
    public async Task SubmitAsync_ValidRequest_ReturnsOkWithRegistrationId()
    {
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessRow());

        var result = await CreateSut().SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Equal(12345, result.Data!.TrialRegistrationId);
        Assert.Equal("TRIAL-KEYCODE", result.Data.Keycode);
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

    [Theory]
    [InlineData("WSAV", 3, "license_category_name must be 'SAEP' for Multi Trial")]
    [InlineData("SAEP", 1, "license_keycode_type_id must be 3 for Multi Trial")]
    public async Task SubmitAsync_UnsupportedPrimarySelectionStopsBeforeDependencies(
        string primaryCategory,
        int keycodeTypeId,
        string expectedError)
    {
        var result = await CreateSut().SubmitAsync(ValidRequest(
            licenseCategoryName: primaryCategory,
            licenseKeycodeTypeId: keycodeTypeId));

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(expectedError, result.ValidationErrors);
        _emailVerificationMock.Verify(v => v.VerifyAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.InsertTrialRegistrationAsync(
            It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _skyriseMock.Verify(s => s.GenerateAsync(
            It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("OTEDR")]
    [InlineData("OTMDR")]
    public async Task SubmitAsync_RemovedEdrMdrModules_ReturnValidationError(string category)
    {
        var request = ValidRequest([new MultiTrialModuleRequest { LicenseCategoryName = category, LicenseSeats = 1 }]);

        var result = await CreateSut().SubmitAsync(request);

        Assert.Equal(ServiceResultKind.ValidationError, result.Kind);
        Assert.Contains(result.ValidationErrors, error =>
            error.StartsWith("modules[0].license_category_name must be one of", StringComparison.Ordinal));
        _repoMock.Verify(r => r.InsertTrialRegistrationAsync(
            It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _skyriseMock.Verify(s => s.GenerateAsync(
            It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()), Times.Never);
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
        var callOrder = new List<string>();
        _emailVerificationMock
            .Setup(v => v.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, _) => callOrder.Add("email"))
            .ReturnsAsync(WebbulaVerificationResult.Allowed());
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<MultiTrialSubmissionRequest, string, CancellationToken>((_, _, _) => callOrder.Add("registration"))
            .ReturnsAsync(SuccessRow());
        _skyriseMock.Setup(s => s.GenerateAsync(
                It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<KeycodeRequest, CancellationToken>((_, _) => callOrder.Add("keycode"))
            .ReturnsAsync(KeycodeResult.Ok("SAEP-KEYCODE"));
        _repoMock.Setup(r => r.InsertTrialAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<int, string?, CancellationToken>((_, keycode, _) => callOrder.Add($"trial:{keycode}"))
            .ReturnsAsync(new TrialInsertProcedureResult { TrialId = 30001, FinalKeycode = "SAEP-KEYCODE" });

        var result = await sut.SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Equal("SAEP-KEYCODE", result.Data!.Keycode);
        Assert.Equal(new[] { "email", "registration", "keycode", "trial:SAEP-KEYCODE" }, callOrder);
        _repoMock.Verify(r => r.InsertTrialAsync(
            12345, "SAEP-KEYCODE", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_SkyriseFailure_InsertsTrialWithNullKeycodeAndReturnsFallback()
    {
        var sut = CreateSut();
        var callOrder = new List<string>();
        _emailVerificationMock
            .Setup(v => v.VerifyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, _) => callOrder.Add("email"))
            .ReturnsAsync(WebbulaVerificationResult.Allowed());
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<MultiTrialSubmissionRequest, string, CancellationToken>((_, _, _) => callOrder.Add("registration"))
            .ReturnsAsync(SuccessRow());
        _skyriseMock.Setup(s => s.GenerateAsync(
                It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()))
            .Callback<KeycodeRequest, CancellationToken>((_, _) => callOrder.Add("keycode"))
            .ReturnsAsync(KeycodeResult.Failed("Skyrise unavailable"));
        _repoMock.Setup(r => r.InsertTrialAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<int, string?, CancellationToken>((_, keycode, _) => callOrder.Add($"trial:{keycode ?? "null"}"))
            .ReturnsAsync(new TrialInsertProcedureResult { TrialId = 30002, FinalKeycode = "DB-FALLBACK-KEY" });

        var result = await sut.SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Ok, result.Kind);
        Assert.Equal("DB-FALLBACK-KEY", result.Data!.Keycode);
        Assert.Equal(new[] { "email", "registration", "keycode", "trial:null" }, callOrder);
        _skyriseMock.Verify(s => s.GenerateAsync(
            It.IsAny<KeycodeRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.InsertTrialAsync(
            12345, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_TrialInsertionReturnsNoResult_ReturnsError()
    {
        var sut = CreateSut();
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessRow());
        _repoMock.Setup(r => r.InsertTrialAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TrialInsertProcedureResult?)null);

        var result = await sut.SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Error, result.Kind);
        Assert.Equal("Trial insert could not be completed", result.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAsync_TrialInsertionThrows_ReturnsError()
    {
        var sut = CreateSut();
        _repoMock.Setup(r => r.InsertTrialRegistrationAsync(
                It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SuccessRow());
        _repoMock.Setup(r => r.InsertTrialAsync(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database operation failed"));

        var result = await sut.SubmitAsync(ValidRequest());

        Assert.Equal(ServiceResultKind.Error, result.Kind);
        Assert.Equal("Trial insert could not be completed", result.ErrorMessage);
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
