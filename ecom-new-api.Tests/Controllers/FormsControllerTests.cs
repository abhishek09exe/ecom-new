using ecom_new_api.Controllers;
using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;
using ecom_new_api.Services.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Controllers;

public sealed class FormsControllerTests
{
    private static FormsController CreateController(
        Mock<IFormSubmissionService> service,
        Dictionary<string, string?> formValues,
        string? contentType = "application/x-www-form-urlencoded")
    {
        var form = new FormCollection(formValues.ToDictionary(
            k => k.Key,
            v => new Microsoft.Extensions.Primitives.StringValues(v.Value ?? string.Empty)));

        var context = new DefaultHttpContext();
        context.Request.ContentType = contentType;
        context.Features.Set<IFormFeature>(new FormFeature(form));

        return new FormsController(service.Object, NullLogger<FormsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }

    [Fact]
    public async Task Submit_NonFormContentType_Returns422()
    {
        var service = new Mock<IFormSubmissionService>();

        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";

        var controller = new FormsController(service.Object, NullLogger<FormsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        var result = await controller.Submit(new FormSubmissionRequest(), CancellationToken.None);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, objectResult.StatusCode);
        service.Verify(s => s.SubmitAsync(
            It.IsAny<FormSubmissionRequest>(),
            It.IsAny<IDictionary<string, string?>>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Submit_ServiceReturnsSuccess_Returns200WithContractPayload()
    {
        var expected = new FormSubmissionResponse
        {
            FormResponseKey = "frm_abc",
            FormSubmitId = 42,
            Entity = new Dictionary<string, object?> { ["keycode"] = "ABC123" }
        };

        var service = new Mock<IFormSubmissionService>();
        service.Setup(s => s.SubmitAsync(
                It.IsAny<FormSubmissionRequest>(),
                It.IsAny<IDictionary<string, string?>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.Ok(expected));

        var controller = CreateController(service, new Dictionary<string, string?>
        {
            ["form_name"] = "ConsumerTrialRegistration"
        });

        var result = await controller.Submit(
            new FormSubmissionRequest { FormName = "ConsumerTrialRegistration" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<FormSubmissionResponse>(ok.Value);
        Assert.Equal("frm_abc", payload.FormResponseKey);
        Assert.Equal(42, payload.FormSubmitId);
    }

    [Fact]
    public async Task Submit_ServiceReturnsValidationErrors_Returns422()
    {
        var service = new Mock<IFormSubmissionService>();
        service.Setup(s => s.SubmitAsync(
                It.IsAny<FormSubmissionRequest>(),
                It.IsAny<IDictionary<string, string?>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.Invalid("customer_email", "Invalid email"));

        var controller = CreateController(service, new Dictionary<string, string?>
        {
            ["form_name"] = "ConsumerTrialRegistration"
        });

        var result = await controller.Submit(new FormSubmissionRequest(), CancellationToken.None);

        Assert.IsType<UnprocessableEntityObjectResult>(result);
    }

    [Fact]
    public async Task Submit_ServiceReturnsError_Returns500()
    {
        var service = new Mock<IFormSubmissionService>();
        service.Setup(s => s.SubmitAsync(
                It.IsAny<FormSubmissionRequest>(),
                It.IsAny<IDictionary<string, string?>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.Error("boom"));

        var controller = CreateController(service, new Dictionary<string, string?>
        {
            ["form_name"] = "ConsumerTrialRegistration"
        });

        var result = await controller.Submit(new FormSubmissionRequest(), CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, status.StatusCode);
    }

    [Fact]
    public async Task Submit_ForwardsRawFormFieldsToService()
    {
        IDictionary<string, string?>? captured = null;

        var service = new Mock<IFormSubmissionService>();
        service.Setup(s => s.SubmitAsync(
                It.IsAny<FormSubmissionRequest>(),
                It.IsAny<IDictionary<string, string?>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<FormSubmissionRequest, IDictionary<string, string?>, string?, CancellationToken>(
                (_, fields, _, _) => captured = fields)
            .ReturnsAsync(FormSubmissionResult.Ok(new FormSubmissionResponse { FormResponseKey = "k" }));

        var controller = CreateController(service, new Dictionary<string, string?>
        {
            ["form_name"] = "ConsumerTrialRegistration",
            ["utm_source"] = "google"
        });

        await controller.Submit(new FormSubmissionRequest(), CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("google", captured!["utm_source"]);
    }
}
