using System.Text.Json;
using ecom_new_api.Controllers;
using ecom_new_api.Helpers;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.Forms;
using ecom_new_api.Repositories.LicenseOptions;
using ecom_new_api.Services;
using ecom_new_api.Services.MultiTrial;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Controllers;

public sealed class MultiTrialControllerTests
{
    private readonly Mock<IMultiTrialSubmissionService> _serviceMock = new();
    private readonly Mock<IFormResponseRepository> _formResponseMock = new();
    private readonly Mock<ILicenseOptionsRepository> _licenseOptionsMock = new();

    private MultiTrialController CreateController()
        => new(_serviceMock.Object, _formResponseMock.Object, _licenseOptionsMock.Object,
            NullLogger<MultiTrialController>.Instance);

    private MultiTrialController CreateFormController(Dictionary<string, StringValues> fields)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Features.Set<IFormFeature>(new FormFeature(new FormCollection(fields)));
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }

    private static MultiTrialSubmissionRequest Request(
        string formName = "BusinessTrialRegistration", string formType = "MULTI") => new()
    {
        FirstName = "John",
        LastName = "Doe",
        CustomerEmail = "john@company.com",
        CompanyName = "Acme Corp",
        Country = "US",
        LicenseCategoryName = "SAEP",
        LicenseSeats = 100,
        FormName = formName,
        FormType = formType,
        Modules = [new MultiTrialModuleRequest { LicenseCategoryName = "SDNS", LicenseSeats = 100 }]
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateSubmission_OnlySuccessfulSubmissionsInvokeNotificationHook(bool success)
    {
        var request = Request();
        var response = new MultiTrialSubmissionResponse { TrialId = 7, TrialRegistrationId = 8, Keycode = "LOCAL-TEST" };
        _serviceMock.Setup(service => service.SubmitAsync(request, default)).ReturnsAsync(success
            ? ServiceResult<MultiTrialSubmissionResponse>.Ok(response)
            : ServiceResult<MultiTrialSubmissionResponse>.Invalid(["Duplicate registration"]));
        var notifications = new Mock<IMultiTrialNotificationService>();
        string? snapshot = null;
        notifications.Setup(service => service.CompleteAsync(request, response, It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), null, default))
            .Callback<MultiTrialSubmissionRequest, MultiTrialSubmissionResponse, string, string?, string?, int?, CancellationToken>(
                (_, _, json, _, _, _, _) => snapshot = json)
            .Returns(Task.CompletedTask);
        var controller = new MultiTrialController(_serviceMock.Object, _formResponseMock.Object,
            _licenseOptionsMock.Object, NullLogger<MultiTrialController>.Instance, notifications.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = Assert.IsAssignableFrom<ObjectResult>(await controller.CreateSubmission(request, default));
        Assert.Equal(success ? StatusCodes.Status201Created : StatusCodes.Status422UnprocessableEntity, result.StatusCode);
        if (success)
        {
            Assert.Same(response, Assert.IsType<ApiResponse<MultiTrialSubmissionResponse>>(result.Value).Data);
            using var document = JsonDocument.Parse(snapshot!);
            Assert.Equal(request.CustomerEmail, document.RootElement.GetProperty("customer_email").GetString());
        }
        else
        {
            notifications.VerifyNoOtherCalls();
        }
    }

    [Theory]
    [InlineData("EmailEnabled")]
    [InlineData("MarketingEnabled")]
    public async Task CreateSubmission_MalformedNotificationConfigurationPreserves201(string malformedSetting)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["MultiTrialNotifications:" + malformedSetting] = "not-a-boolean"
        }).Build();
        using var provider = new ServiceCollection()
            .Configure<MultiTrialNotificationOptions>(configuration.GetSection("MultiTrialNotifications"))
            .BuildServiceProvider();
        var licenses = new Mock<ILicenseOptionsRepository>(MockBehavior.Strict);
        var forms = new Mock<IFormResponseRepository>(MockBehavior.Strict);
        var queue = new Mock<ecom_new_api.Repositories.Trials.IMultiTrialNotificationRepository>(MockBehavior.Strict);
        var notifications = new MultiTrialNotificationService(
            provider.GetRequiredService<IOptions<MultiTrialNotificationOptions>>(), licenses.Object, forms.Object,
            queue.Object, NullLogger<MultiTrialNotificationService>.Instance);
        var request = Request();
        var response = new MultiTrialSubmissionResponse { TrialId = 7, TrialRegistrationId = 8, Keycode = "LOCAL-TEST" };
        _serviceMock.Setup(service => service.SubmitAsync(request, default))
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Ok(response));
        var controller = new MultiTrialController(_serviceMock.Object, forms.Object, licenses.Object,
            NullLogger<MultiTrialController>.Instance, notifications)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = Assert.IsType<ObjectResult>(await controller.CreateSubmission(request, default));
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Same(response, Assert.IsType<ApiResponse<MultiTrialSubmissionResponse>>(result.Value).Data);
        licenses.VerifyNoOtherCalls();
        forms.VerifyNoOtherCalls();
        queue.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateSubmission_EmailNotificationFailurePreserves201()
    {
        var request = Request();
        var response = new MultiTrialSubmissionResponse { TrialId = 7, TrialRegistrationId = 8, Keycode = "LOCAL-TEST" };
        _serviceMock.Setup(service => service.SubmitAsync(request, default))
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Ok(response));
        _licenseOptionsMock.Setup(repository => repository.SelectLicenseOptionsAsync("LOCAL-TEST", null, default))
            .ThrowsAsync(new InvalidOperationException("Local notification failure"));
        var queue = new Mock<ecom_new_api.Repositories.Trials.IMultiTrialNotificationRepository>(MockBehavior.Strict);
        var notifications = new MultiTrialNotificationService(
            Options.Create(new MultiTrialNotificationOptions { EmailEnabled = true }),
            _licenseOptionsMock.Object, _formResponseMock.Object, queue.Object,
            NullLogger<MultiTrialNotificationService>.Instance);
        var controller = new MultiTrialController(_serviceMock.Object, _formResponseMock.Object,
            _licenseOptionsMock.Object, NullLogger<MultiTrialController>.Instance, notifications)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = Assert.IsType<ObjectResult>(await controller.CreateSubmission(request, default));
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Same(response, Assert.IsType<ApiResponse<MultiTrialSubmissionResponse>>(result.Value).Data);
        queue.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateSubmission_ValidRequest_Returns201WithEnvelope()
    {
        var payload = new MultiTrialSubmissionResponse
        {
            TrialRegistrationId = 12345,
            FormType = "MULTI",
            LicenseCategoryName = "SAEP"
        };
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Ok(payload));

        var result = await CreateController().CreateSubmission(Request(), CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var envelope = Assert.IsType<ApiResponse<MultiTrialSubmissionResponse>>(created.Value);
        Assert.Equal(0, envelope.ResponseCode);
        Assert.Equal(12345, envelope.Data!.TrialRegistrationId);
        _formResponseMock.Verify(r => r.InsertFormSubmitAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _formResponseMock.Verify(r => r.InsertFormResponseAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _licenseOptionsMock.Verify(r => r.SelectLicenseOptionsAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSubmission_ValidationError_Returns422WithFieldErrorArrays()
    {
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Invalid(["customer_email is required"]));

        var result = await CreateController().CreateSubmission(Request(), CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var body = JsonSerializer.SerializeToElement(unprocessable.Value);
        var errors = body.GetProperty("errors");
        Assert.Equal("customer_email is required", errors.GetProperty("customer_email")[0].GetString());
    }

    [Fact]
    public async Task CreateSubmission_NonFieldValidationError_UsesRequestErrorKey()
    {
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Invalid(["Duplicate registration"]));

        var result = await CreateController().CreateSubmission(Request(), CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var body = JsonSerializer.SerializeToElement(unprocessable.Value);
        Assert.Equal("Duplicate registration", body.GetProperty("errors").GetProperty("request")[0].GetString());
    }

    [Theory]
    [InlineData("wrong", "MULTI", "form_name")]
    [InlineData("BusinessTrialRegistration", "WRONG", "form_type")]
    public async Task CreateSubmission_InvalidFormMetadata_Returns422(string formName, string formType, string field)
    {
        var request = Request(formName, formType);

        var result = await CreateController().CreateSubmission(request, CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var body = JsonSerializer.SerializeToElement(unprocessable.Value);
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _));
        _serviceMock.Verify(s => s.SubmitAsync(
            It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSubmission_ModelBindingErrors_Returns422WithNormalizedModuleField()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Modules[0].LicenseSeats", "must be positive");

        var result = await controller.CreateSubmission(Request(), CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var body = JsonSerializer.SerializeToElement(unprocessable.Value);
        Assert.Equal("must be positive", body.GetProperty("errors")
            .GetProperty("modules[0][license_seats]")[0].GetString());
        _serviceMock.Verify(s => s.SubmitAsync(
            It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void CreateSubmission_ExposesLegacyRouteAndJsonContentType()
    {
        var action = typeof(MultiTrialController).GetMethod(nameof(MultiTrialController.CreateSubmission))!;
        var postRoutes = action.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true)
            .Cast<HttpPostAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();
        var consumes = action.GetCustomAttributes(typeof(ConsumesAttribute), inherit: true)
            .Cast<ConsumesAttribute>()
            .Single();

        Assert.Contains("~/forms/submissions", postRoutes);
        Assert.Contains("application/json", consumes.ContentTypes);

        var formAction = typeof(MultiTrialController).GetMethod(nameof(MultiTrialController.CreateFormSubmission))!;
        var formPost = Assert.Single(formAction.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true)
            .Cast<HttpPostAttribute>());
        var formConsumes = Assert.Single(formAction.GetCustomAttributes(typeof(ConsumesAttribute), inherit: true)
            .Cast<ConsumesAttribute>());
        var apiExplorer = Assert.Single(formAction.GetCustomAttributes(typeof(ApiExplorerSettingsAttribute), inherit: true)
            .Cast<ApiExplorerSettingsAttribute>());
        Assert.Equal("~/forms/submissions", formPost.Template);
        Assert.Contains("application/x-www-form-urlencoded", formConsumes.ContentTypes);
        Assert.True(apiExplorer.IgnoreApi);
    }

    [Fact]
    public async Task CreateFormSubmission_ValidLegacyFormBindsOrderedModulesAndStringNumbers()
    {
        MultiTrialSubmissionRequest? captured = null;
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MultiTrialSubmissionRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Ok(new MultiTrialSubmissionResponse
            {
                TrialId = 345,
                TrialRegistrationId = 12345,
                Keycode = "SAEP-KEYCODE",
                FormType = "MULTI",
                LicenseCategoryName = "SAEP"
            }));
        _licenseOptionsMock.Setup(r => r.SelectLicenseOptionsAsync(
                "SAEP-KEYCODE", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LicenseOptionsResponse
            {
                LicenseId = 456,
                Keycode = "SAEP-KEYCODE",
                LicenseKey = "license-guid"
            });
        _formResponseMock.Setup(r => r.InsertFormSubmitAsync(
                "BusinessTrialRegistration", It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(789);
        string? savedSnapshot = null;
        _formResponseMock.Setup(r => r.InsertFormResponseAsync(
                789, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<int, string, CancellationToken>((_, json, _) => savedSnapshot = json)
            .ReturnsAsync("12345678-1234-1234-1234-123456789012");

        var result = await CreateFormController(new Dictionary<string, StringValues>
        {
            ["form_name"] = "BusinessTrialRegistration",
            ["form_type"] = "MULTI",
            ["first_name"] = "Ada",
            ["last_name"] = "Lovelace",
            ["customer_email"] = "ada@example.com",
            ["company_name"] = "Example Co",
            ["country"] = "US",
            ["license_category_name"] = "SAEP",
            ["license_seats"] = "100",
            ["modules[0][license_category_name]"] = "SDNS",
            ["modules[0][license_seats]"] = "100",
            ["modules[1][license_category_name]"] = "SECA",
            ["modules[1][license_seats]"] = "25",
            ["SMBC_Email_Opt_In__c"] = "on",
            ["optInCompanyAndProductCommunication1"] = "1",
            ["m_campaign_id"] = "tracking-only"
        }).CreateFormSubmission(CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var legacy = Assert.IsType<LegacyFormSubmissionResponse>(created.Value);
        Assert.Equal("12345678-1234-1234-1234-123456789012", legacy.FormResponseKey);
        Assert.Equal(789, legacy.FormSubmitId);
        Assert.Equal(456, legacy.Entity.LicenseId);
        Assert.Equal(12345, legacy.Entity.TrialRegistrationId);
        Assert.Equal(345, legacy.Entity.Trial!.TrialId);
        Assert.Equal("license-guid", legacy.Entity.Trial.LicenseKey);
        Assert.Equal("SAEP-KEYCODE", legacy.Entity.Trial.Keycode);
        Assert.Equal("MULTI", legacy.Entity.FormType);
        Assert.NotNull(captured);
        Assert.Equal("Ada", captured!.FirstName);
        Assert.Equal(100, captured.LicenseSeats);
        Assert.Equal(30, captured.TrialDays);
        Assert.Null(captured.OptIn);
        Assert.Null(captured.Address1);
        Assert.Null(captured.CompanyTypeId);
        Assert.Null(captured.PartnerKey);
        Assert.Collection(captured.Modules,
            module =>
            {
                Assert.Equal("SDNS", module.LicenseCategoryName);
                Assert.Equal(100, module.LicenseSeats);
                Assert.Null(module.VaultId);
            },
            module =>
            {
                Assert.Equal("SECA", module.LicenseCategoryName);
                Assert.Equal(25, module.LicenseSeats);
                Assert.Null(module.VaultId);
            });

        _formResponseMock.Verify(r => r.InsertFormSubmitAsync(
            "BusinessTrialRegistration", It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _formResponseMock.Verify(r => r.InsertFormResponseAsync(
            789, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        using var snapshot = JsonDocument.Parse(savedSnapshot!);
        var snapshotRoot = snapshot.RootElement;
        Assert.Equal(789, snapshotRoot.GetProperty("form_submit_id").GetInt32());
        var snapshotEntity = snapshotRoot.GetProperty("entity");
        Assert.Equal("MULTI", snapshotEntity.GetProperty("form_type").GetString());
        Assert.Equal("SAEP-KEYCODE", snapshotEntity.GetProperty("trial").GetProperty("keycode").GetString());
        Assert.Equal("license-guid", snapshotEntity.GetProperty("trial").GetProperty("license_key").GetString());
    }

    [Fact]
    public async Task CreateFormSubmission_MissingPersistedResponseKeyDoesNotReturnSuccess()
    {
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Ok(new MultiTrialSubmissionResponse
            {
                TrialId = 345,
                TrialRegistrationId = 12345,
                Keycode = "SAEP-KEYCODE",
                FormType = "MULTI",
                LicenseCategoryName = "SAEP"
            }));
        _licenseOptionsMock.Setup(r => r.SelectLicenseOptionsAsync(
                "SAEP-KEYCODE", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LicenseOptionsResponse
            {
                LicenseId = 456,
                Keycode = "SAEP-KEYCODE",
                LicenseKey = "license-guid"
            });
        _formResponseMock.Setup(r => r.InsertFormSubmitAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(789);
        _formResponseMock.Setup(r => r.InsertFormResponseAsync(
                789, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await CreateFormController(new Dictionary<string, StringValues>
        {
            ["form_name"] = "BusinessTrialRegistration",
            ["form_type"] = "MULTI",
            ["customer_email"] = "ada@example.com",
            ["license_category_name"] = "SAEP",
            ["license_seats"] = "1"
        }).CreateFormSubmission(CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task CreateFormSubmission_FailedSubmitInsertDoesNotInsertResponse()
    {
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Ok(new MultiTrialSubmissionResponse
            {
                TrialId = 345,
                TrialRegistrationId = 12345,
                Keycode = "SAEP-KEYCODE",
                FormType = "MULTI",
                LicenseCategoryName = "SAEP"
            }));
        _licenseOptionsMock.Setup(r => r.SelectLicenseOptionsAsync(
                "SAEP-KEYCODE", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LicenseOptionsResponse
            {
                LicenseId = 456,
                Keycode = "SAEP-KEYCODE",
                LicenseKey = "license-guid"
            });
        _formResponseMock.Setup(r => r.InsertFormSubmitAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int?)null);

        var result = await CreateFormController(new Dictionary<string, StringValues>
        {
            ["form_name"] = "BusinessTrialRegistration",
            ["form_type"] = "MULTI",
            ["customer_email"] = "ada@example.com",
            ["license_category_name"] = "SAEP",
            ["license_seats"] = "1"
        }).CreateFormSubmission(CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, Assert.IsType<ObjectResult>(result).StatusCode);
        _formResponseMock.Verify(r => r.InsertFormResponseAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateFormSubmission_MissingRequiredFieldsUsesExisting422Errors()
    {
        MultiTrialSubmissionRequest? captured = null;
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MultiTrialSubmissionRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Invalid(["customer_email is required"]));

        var result = await CreateFormController(new Dictionary<string, StringValues>
        {
            ["form_name"] = "BusinessTrialRegistration",
            ["form_type"] = "MULTI"
        }).CreateFormSubmission(CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var errors = JsonSerializer.SerializeToElement(unprocessable.Value).GetProperty("errors");
        Assert.Equal("customer_email is required", errors.GetProperty("customer_email")[0].GetString());
        Assert.NotNull(captured);
        Assert.Null(captured!.CustomerEmail);
        _serviceMock.Verify(s => s.SubmitAsync(
            It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFormSubmission_InvalidScalarOrModuleNumberReturns422WithoutCallingService()
    {
        var result = await CreateFormController(new Dictionary<string, StringValues>
        {
            ["form_name"] = "BusinessTrialRegistration",
            ["form_type"] = "MULTI",
            ["license_seats"] = "many",
            ["modules[0][license_seats]"] = "12x"
        }).CreateFormSubmission(CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var errors = JsonSerializer.SerializeToElement(unprocessable.Value).GetProperty("errors");
        Assert.Equal("The request value is invalid.", errors.GetProperty("license_seats")[0].GetString());
        Assert.Equal("The request value is invalid.", errors.GetProperty("modules[0][license_seats]")[0].GetString());
        _serviceMock.Verify(s => s.SubmitAsync(
            It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void RequestDto_BindsSnakeCaseJsonModulesAndVault()
    {
        const string payload = """
            {
              "form_name": "BusinessTrialRegistration",
              "form_type": "MULTI",
              "license_category_name": "SAEP",
              "license_seats": 12,
                            "modules": [
                                { "license_category_name": "SECA", "license_seats": 8, "vault_id": 131 }
                            ]
            }
            """;
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
            PropertyNameCaseInsensitive = true
        };

        var request = JsonSerializer.Deserialize<MultiTrialSubmissionRequest>(payload, options)!;

        Assert.Equal("BusinessTrialRegistration", request.FormName);
        Assert.Equal("MULTI", request.FormType);
        Assert.Equal(12, request.LicenseSeats);
        var module = Assert.Single(request.Modules);
        Assert.Equal("SECA", module.LicenseCategoryName);
        Assert.Equal(8, module.LicenseSeats);
        Assert.Equal(131, module.VaultId);
    }

        [Fact]
        public void RequestDto_BindsFrontendQuotedNumbersAndDefaultsTrialDays()
        {
                const string payload = """
                        {
                            "form_name": "BusinessTrialRegistration",
                            "form_type": "MULTI",
                            "license_category_name": "SAEP",
                            "license_seats": "10",
                            "is_saep_selected": "1",
                            "modules": [
                                { "license_category_name": "SECA", "license_seats": "1", "vault_id": "131" }
                            ]
                        }
                        """;
                var options = new JsonSerializerOptions
                {
                        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
                        PropertyNameCaseInsensitive = true
                };

                var request = JsonSerializer.Deserialize<MultiTrialSubmissionRequest>(payload, options)!;

                Assert.Equal(10, request.LicenseSeats);
                Assert.Equal(30, request.TrialDays);
                var module = Assert.Single(request.Modules);
                Assert.Equal(1, module.LicenseSeats);
                Assert.Equal(131, module.VaultId);
        }

        [Fact]
        public void RequestDto_MissingPrimarySeatDefaultsToZeroForServiceValidation()
        {
                const string payload = """
                        {
                            "form_name": "BusinessTrialRegistration",
                            "form_type": "MULTI",
                            "license_category_name": "SAEP",
                            "modules": []
                        }
                        """;
                var options = new JsonSerializerOptions
                {
                        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
                        PropertyNameCaseInsensitive = true
                };

                var request = JsonSerializer.Deserialize<MultiTrialSubmissionRequest>(payload, options)!;

                Assert.Equal(0, request.LicenseSeats);
                Assert.Equal(30, request.TrialDays);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("\"\"")]
        [InlineData("\"ten\"")]
        public void RequestDto_InvalidPrimarySeatValuesFailJsonBinding(string value)
        {
                var payload = $$"""
                        {
                            "form_name": "BusinessTrialRegistration",
                            "form_type": "MULTI",
                            "license_category_name": "SAEP",
                            "license_seats": {{value}},
                            "modules": []
                        }
                        """;
                var options = new JsonSerializerOptions
                {
                        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
                        PropertyNameCaseInsensitive = true
                };

                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MultiTrialSubmissionRequest>(payload, options));
        }

        [Theory]
        [InlineData("null")]
        [InlineData("\"\"")]
        [InlineData("\"not-a-number\"")]
        public void RequestDto_InvalidModuleSeatValuesFailJsonBinding(string value)
        {
                var payload = $$"""
                        {
                            "form_name": "BusinessTrialRegistration",
                            "form_type": "MULTI",
                            "license_category_name": "SAEP",
                            "license_seats": "10",
                            "modules": [
                                { "license_category_name": "SECA", "license_seats": {{value}} }
                            ]
                        }
                        """;
                var options = new JsonSerializerOptions
                {
                        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
                        PropertyNameCaseInsensitive = true
                };

                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MultiTrialSubmissionRequest>(payload, options));
        }

        [Fact]
        public void RequestDto_OptionalVaultAllowsMissingNullAndZero()
        {
                const string payload = """
                        {
                            "license_category_name": "SAEP",
                            "modules": [
                                { "license_category_name": "SDNS", "license_seats": "1" },
                                { "license_category_name": "SECA", "license_seats": "1", "vault_id": null },
                                { "license_category_name": "SDNS", "license_seats": "1", "vault_id": "0" }
                            ]
                        }
                        """;
                var options = new JsonSerializerOptions
                {
                        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
                        PropertyNameCaseInsensitive = true
                };

                var request = JsonSerializer.Deserialize<MultiTrialSubmissionRequest>(payload, options)!;

                Assert.Null(request.Modules[0].VaultId);
                Assert.Null(request.Modules[1].VaultId);
                Assert.Equal(0, request.Modules[2].VaultId);
        }

        [Theory]
        [InlineData("\"\"")]
        [InlineData("\"north\"")]
        public void RequestDto_EmptyOrInvalidVaultFailsJsonBinding(string value)
        {
                var payload = $$"""
                        {
                            "license_category_name": "SAEP",
                            "modules": [
                                { "license_category_name": "SECA", "license_seats": "1", "vault_id": {{value}} }
                            ]
                        }
                        """;
                var options = new JsonSerializerOptions
                {
                        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
                        PropertyNameCaseInsensitive = true
                };

                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<MultiTrialSubmissionRequest>(payload, options));
        }

    [Fact]
    public async Task CreateSubmission_ServiceError_Returns500()
    {
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Error("Trial registration failed"));

        var result = await CreateController().CreateSubmission(Request(), CancellationToken.None);

        var error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, error.StatusCode);
    }

    [Fact]
    public async Task CreateFormSubmission_InvalidModuleCategoryUsesIndexed422Field()
    {
        MultiTrialSubmissionRequest? captured = null;
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<MultiTrialSubmissionRequest, CancellationToken>((request, _) => captured = request)
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Invalid(
                ["modules[0].license_category_name must be one of: SDNS, SECA"]));

        var result = await CreateFormController(new Dictionary<string, StringValues>
        {
            ["form_name"] = "BusinessTrialRegistration",
            ["form_type"] = "MULTI",
            ["modules[0][license_category_name]"] = "NOT-A-MODULE",
            ["modules[0][license_seats]"] = "1"
        }).CreateFormSubmission(CancellationToken.None);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(result);
        var errors = JsonSerializer.SerializeToElement(unprocessable.Value).GetProperty("errors");
        Assert.Equal("modules[0].license_category_name must be one of: SDNS, SECA",
            errors.GetProperty("modules[0][license_category_name]")[0].GetString());
        Assert.Equal("NOT-A-MODULE", Assert.Single(captured!.Modules).LicenseCategoryName);
    }
}
