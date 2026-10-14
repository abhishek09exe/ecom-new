namespace ecom_new_api.Repositories.Forms;

public interface IFormResponseRepository
{
    Task<int?> InsertFormSubmitAsync(
        string formName,
        string? ipAddress,
        string? formUrl,
        string formJson,
        CancellationToken ct = default);

    Task<string?> InsertFormResponseAsync(
        int formSubmitId,
        string responseJson,
        CancellationToken ct = default);

    Task<FormResponseSnapshot?> GetFormResponseAsync(
        string responseKey,
        CancellationToken ct = default);
}

public sealed record FormResponseSnapshot(
    int FormResponseId,
    int FormSubmitId,
    string FormResponseKey,
    string FormResponseJson,
    DateTime InsertDate);
