namespace ecom_new_api.Services.EmailVerification;

public interface IEmailVerificationService
{
    Task<WebbulaVerificationResult> VerifyAsync(string email, CancellationToken ct = default);
}

public sealed record WebbulaVerificationResult(bool IsExplicitlyInvalid)
{
    public static WebbulaVerificationResult Allowed() => new(false);

    public static WebbulaVerificationResult Invalid() => new(true);
}