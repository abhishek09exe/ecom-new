namespace ecom_new_api.Middleware;

using ecom_new_api.Models.Responses;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

/// <summary>
/// Global exception handler middleware.
/// 
/// Catches unhandled exceptions from service/repository layers and converts them to
/// appropriate HTTP responses without exposing internal stack traces to clients.
/// 
/// Usage: app.UseMiddleware<ExceptionHandlerMiddleware>() in Program.cs
/// </summary>
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Middleware invocation: wraps next middleware in try-catch.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Call next middleware in pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log full exception details server-side
            _logger.LogError(ex,
                "Unhandled exception: {ExceptionType} - {Message}",
                ex.GetType().Name, ex.Message);

            // Convert to appropriate HTTP response
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Maps exception types to HTTP status codes and error responses.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Map exception types to status codes
        var (statusCode, message) = exception switch
        {
            // Database-related exceptions
            InvalidOperationException when exception.Message.Contains("DbContext") =>
                (HttpStatusCode.InternalServerError, "Database connection error"),

            // Validation/argument exceptions
            ArgumentNullException or ArgumentException =>
                (HttpStatusCode.BadRequest, "Invalid request parameter"),

            // General application exceptions
            Exception => (HttpStatusCode.InternalServerError, "Internal server error")
        };

        context.Response.StatusCode = (int)statusCode;

        // Return error response (never expose stack trace to client)
        var response = ApiResponse<object>.Failure(
            message: message,
            errors: new List<string> { exception.GetType().Name }
        );

        return context.Response.WriteAsJsonAsync(response);
    }
}
