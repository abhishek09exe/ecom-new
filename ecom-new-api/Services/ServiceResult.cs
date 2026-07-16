namespace ecom_new_api.Services;

/// <summary>
/// Discriminated result returned by service methods.
/// Keeps HTTP concerns out of the service layer.
/// </summary>
public sealed class ServiceResult<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public List<string> ValidationErrors { get; private init; } = [];
    public string? ErrorMessage { get; private init; }
    public ServiceResultKind Kind { get; private init; }

    public static ServiceResult<T> Ok(T data) => new()
    {
        IsSuccess = true,
        Data = data,
        Kind = ServiceResultKind.Ok
    };

    public static ServiceResult<T> Invalid(List<string> errors) => new()
    {
        IsSuccess = false,
        ValidationErrors = errors,
        Kind = ServiceResultKind.ValidationError
    };

    public static ServiceResult<T> NotFound(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        Kind = ServiceResultKind.NotFound
    };

    public static ServiceResult<T> Error(string message) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        Kind = ServiceResultKind.Error
    };
}

public enum ServiceResultKind { Ok, ValidationError, NotFound, Error }
