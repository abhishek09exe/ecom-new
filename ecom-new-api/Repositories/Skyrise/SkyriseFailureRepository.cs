using ecom_new_api.Data;
using ecom_new_api.Data.Entities;

namespace ecom_new_api.Repositories.Skyrise;

/// <summary>
/// EF Core persistence for the skyrise_license_failure audit table. Mirrors PHP's
/// SkyriseFailure::create(['skyrise_request' => ..., 'skyrise_failure_message' => ...]).
/// </summary>
public sealed class SkyriseFailureRepository : ISkyriseFailureRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<SkyriseFailureRepository> _logger;

    public SkyriseFailureRepository(AppDbContext db, ILogger<SkyriseFailureRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RecordFailureAsync(string requestJson, string failureMessageJson, CancellationToken ct = default)
    {
        try
        {
            _db.SkyriseFailure.Add(new SkyriseFailure
            {
                SkyriseRequest = requestJson,
                SkyriseFailureMessage = failureMessageJson,
                FailureDate = DateTime.UtcNow,
            });

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            // Auditing must never take down the caller — log and swallow.
            _logger.LogError(ex, "Failed to persist Skyrise failure audit row.");
        }
    }
}
