using ecom_new_api.Controllers;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Services;
using ecom_new_api.Services.MultiTrial;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Controllers;

public sealed class MultiTrialControllerTests
{
    private readonly Mock<IMultiTrialSubmissionService> _serviceMock = new();

    private MultiTrialController CreateController()
        => new(_serviceMock.Object, NullLogger<MultiTrialController>.Instance);

    private static MultiTrialSubmissionRequest Request() => new()
    {
        FirstName = "John",
        LastName = "Doe",
        CustomerEmail = "john@company.com",
        CompanyName = "Acme Corp",
        Country = "US",
        LicenseCategoryName = "SAEP",
        LicenseSeats = 100,
        Modules = [new MultiTrialModuleRequest { LicenseCategoryName = "SDNS", LicenseSeats = 100 }]
    };

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
    }

    [Fact]
    public async Task CreateSubmission_ValidationError_Returns400WithErrors()
    {
        _serviceMock.Setup(s => s.SubmitAsync(It.IsAny<MultiTrialSubmissionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<MultiTrialSubmissionResponse>.Invalid(["customer_email is required"]));

        var result = await CreateController().CreateSubmission(Request(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var envelope = Assert.IsType<ApiResponse<MultiTrialSubmissionResponse>>(badRequest.Value);
        Assert.Contains("customer_email is required", envelope.Errors);
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
}
