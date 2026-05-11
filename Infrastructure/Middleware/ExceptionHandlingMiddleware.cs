using System.Runtime.ExceptionServices;
using System.Text.Json;
using AllMarket.Infrastructure.Exceptions;
using AllMarket.Infrastructure.Responses;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Infrastructure.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    // Builds the final HTTP response.
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            logger.LogWarning(exception, "The response has already started. Exception handling middleware cannot write an error response.");
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        var (statusCode, errorCode, message) = MapException(exception);

        LogException(exception, statusCode);

        var response = new ErrorResponse
        {
            Error = errorCode,
            Message = message,
            StatusCode = statusCode,
            TraceId = context.TraceIdentifier
        };

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await WriteJsonResponseAsync(context, response);
    }

    // Expected domain errors are warnings; unexpected server failures are errors.
    private void LogException(Exception exception, int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception.");
            return;
        }

        logger.LogWarning(exception, "Handled application exception.");
    }

    // Serializes every error response with the same JSON casing as the public API.
    private static async Task WriteJsonResponseAsync(HttpContext context, ErrorResponse response)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    private (int StatusCode, string ErrorCode, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            AppException appException => (appException.StatusCode, appException.ErrorCode, appException.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "concurrency_conflict", "The resource was modified by another operation."),
            DbUpdateException => (StatusCodes.Status409Conflict, "database_conflict", "The request conflicts with the current database state."),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "invalid_operation", exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "invalid_argument", exception.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "internal_server_error",
                environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.")
        };
    }
}
