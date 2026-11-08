using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Common;
using TaskFlow.Domain;

namespace TaskFlow.Api.Errors;

/// <summary>
/// Maps the application's exceptions to RFC 9457 problem details. Anything it does not
/// recognize is left to the default handler (a 500 that does not expose the exception).
/// </summary>
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title)? mapping = exception switch
        {
            DomainValidationException or ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => null,
        };

        if (mapping is not { } map)
        {
            return false;
        }

        var problem = new ProblemDetails { Status = map.Status, Title = map.Title, Detail = exception.Message };

        httpContext.Response.StatusCode = map.Status;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }
}
