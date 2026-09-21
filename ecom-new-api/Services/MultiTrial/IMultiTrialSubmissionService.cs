using ecom_new_api.Models.Requests;
using ecom_new_api.Models.Responses;

namespace ecom_new_api.Services.MultiTrial;

public interface IMultiTrialSubmissionService
{
    Task<ServiceResult<MultiTrialSubmissionResponse>> SubmitAsync(
        MultiTrialSubmissionRequest request,
        CancellationToken ct = default);
}
