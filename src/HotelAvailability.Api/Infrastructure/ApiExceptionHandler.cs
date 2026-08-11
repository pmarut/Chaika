using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HotelAvailability.Api.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // ASP.NET Core only auto-converts a failed JSON body bind (missing required field,
        // malformed JSON) into a 400 outside the Development environment; in Development
        // (which includes the WebApplicationFactory test host) it throws BadHttpRequestException
        // instead, so it's translated here to keep the 400 behavior environment-independent.
        var problemDetails = exception is BadHttpRequestException badHttpRequestException
            ? new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{badHttpRequestException.StatusCode}",
                Title = "Bad Request",
                Status = badHttpRequestException.StatusCode,
                Detail = badHttpRequestException.Message
            }
            : new ProblemDetails
            {
                Type = "https://httpstatuses.com/500",
                Title = "An unexpected error occurred.",
                Status = StatusCodes.Status500InternalServerError
            };

        logger.LogError(
            exception,
            "Exception while processing {Method} {Path}, mapped to {StatusCode}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            problemDetails.Status);

        httpContext.Response.StatusCode = problemDetails.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
