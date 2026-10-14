using ecom_new_api.Controllers;
using ecom_new_api.Models.Responses;
using ecom_new_api.Repositories.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ecom_new_api_tests.Controllers;

public sealed class FormResponsesControllerTests
{
    private const string ResponseKey = "12345678-1234-1234-1234-123456789012";
    private readonly Mock<IFormResponseRepository> _repositoryMock = new();

    private FormResponsesController CreateController()
        => new(_repositoryMock.Object, NullLogger<FormResponsesController>.Instance);

    [Fact]
    public async Task GetFormResponse_ReturnsStoredLegacySnapshotAndMetadata()
    {
        var insertDate = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);
        _repositoryMock.Setup(r => r.GetFormResponseAsync(ResponseKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FormResponseSnapshot(
                31,
                789,
                ResponseKey,
                """{"form_submit_id":789,"entity":{"form_type":"MULTI"}}""",
                insertDate));

        var result = await CreateController().GetFormResponse(ResponseKey, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LegacyFormResponseLookupResponse>(ok.Value);
        Assert.Equal(31, response.FormResponseId);
        Assert.Equal(789, response.FormSubmitId);
        Assert.Equal(ResponseKey, response.FormResponseKey);
        Assert.Equal(insertDate, response.InsertDate);
        Assert.Equal(789, response.FormResponse.GetProperty("form_submit_id").GetInt32());
        Assert.Equal("MULTI", response.FormResponse.GetProperty("entity").GetProperty("form_type").GetString());
    }

    [Fact]
    public async Task GetFormResponse_InvalidKeyReturns404WithoutRepositoryCall()
    {
        var result = await CreateController().GetFormResponse("not-a-guid", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        _repositoryMock.Verify(r => r.GetFormResponseAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFormResponse_MissingSnapshotReturns404()
    {
        _repositoryMock.Setup(r => r.GetFormResponseAsync(ResponseKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FormResponseSnapshot?)null);

        var result = await CreateController().GetFormResponse(ResponseKey, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetFormResponse_MalformedStoredJsonReturns500()
    {
        _repositoryMock.Setup(r => r.GetFormResponseAsync(ResponseKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FormResponseSnapshot(31, 789, ResponseKey, "not-json", DateTime.UtcNow));

        var result = await CreateController().GetFormResponse(ResponseKey, CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task GetFormResponse_RepositoryFailureReturns500()
    {
        _repositoryMock.Setup(r => r.GetFormResponseAsync(ResponseKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var result = await CreateController().GetFormResponse(ResponseKey, CancellationToken.None);

        Assert.Equal(StatusCodes.Status500InternalServerError, Assert.IsType<ObjectResult>(result).StatusCode);
    }
}