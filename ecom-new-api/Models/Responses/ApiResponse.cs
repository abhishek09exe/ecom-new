namespace ecom_new_api.Models.Responses;

/// <summary>
/// Wraps all API responses with a consistent envelope.
/// On success: Data is populated, Errors is empty.
/// On failure: Data is null, Errors contains human-readable messages.
/// ResponseCode mirrors the stored procedure convention: 0 = success, -200 = error.
/// </summary>
public sealed class ApiResponse<T>
{
    public int ResponseCode { get; init; }
    public string Message { get; init; } = default!;
    public T? Data { get; init; }
    public List<string> Errors { get; init; } = [];

    public static ApiResponse<T> Success(T data) => new()
    {
        ResponseCode = 0,
        Message = "success",
        Data = data
    };

    public static ApiResponse<T> Failure(string message, List<string>? errors = null) => new()
    {
        ResponseCode = -200,
        Message = message,
        Errors = errors ?? []
    };

    public static ApiResponse<T> ValidationFailure(List<string> errors) => new()
    {
        ResponseCode = -200,
        Message = "Validation failed",
        Errors = errors
    };
}
