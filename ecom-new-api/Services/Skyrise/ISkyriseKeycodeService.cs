namespace ecom_new_api.Services.Skyrise;

/// <summary>
/// Input for a single SkyRise keycode generation, mirroring the legacy
/// TrialRegistration `$keycodeRequestData['requests'][0]` payload.
/// </summary>
public sealed class KeycodeGenerationRequest
{
    public string? LicenseDistCode { get; init; }
    public string? LicenseCategory { get; init; }
    public int Storage { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public int DurationInDays { get; init; }
    public int Seats { get; init; }
    public bool IsTrial { get; init; } = true;
    public int? LicenseTypeId { get; init; }
    public int? LicenseCategoryId { get; init; }
    public int? LicenseKeycodeTypeId { get; init; }
    public string? Iso { get; init; }
    public string? LicenseModules { get; init; }
}

/// <summary>Result of a keycode generation attempt.</summary>
public sealed record KeycodeGenerationResult(string? Keycode, string? ErrorMessage)
{
    public bool Success => !string.IsNullOrWhiteSpace(Keycode);

    public static KeycodeGenerationResult Ok(string keycode) => new(keycode, null);
    public static KeycodeGenerationResult Failed(string error) => new(null, error);
}

public interface ISkyriseKeycodeService
{
    /// <summary>
    /// Generates a SkyRise keycode (license or template key) for a trial.
    /// Returns a failure result rather than throwing — ecom remains the fallback generator.
    /// </summary>
    Task<KeycodeGenerationResult> GenerateAsync(KeycodeGenerationRequest request, CancellationToken ct = default);
}
