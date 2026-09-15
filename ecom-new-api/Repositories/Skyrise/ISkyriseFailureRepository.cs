using ecom_new_api.Data;
using ecom_new_api.Data.Entities;

namespace ecom_new_api.Repositories.Skyrise;

/// <summary>
/// Data-access contract for persisting Skyrise keycode-generation failures for audit purposes.
/// </summary>
public interface ISkyriseFailureRepository
{
    Task RecordFailureAsync(string requestJson, string failureMessageJson, CancellationToken ct = default);
}
