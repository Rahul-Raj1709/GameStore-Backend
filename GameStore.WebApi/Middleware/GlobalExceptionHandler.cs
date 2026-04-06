using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace GameStore.WebApi.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. Log the error (minimal logging for performance)
        logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        // 2. Handle Validation Exceptions gracefully
        if (exception is ValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            var validationErrors = validationException.Errors
                .Select(e => new { e.PropertyName, e.ErrorMessage });

            // Return a minimal JSON payload
            await httpContext.Response.WriteAsJsonAsync(new
            {
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Errors = validationErrors
            }, cancellationToken);

            return true; // Tells .NET the exception is handled
        }

        // 3. Catch-all for unhandled server errors
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError
        }, cancellationToken);

        return true;
    }
}