using Microsoft.AspNetCore.Diagnostics;

namespace TaskFlow.Api.Errors;

/// <summary>
/// Maps the application's exceptions to RFC 9457 problem details. Anything it does not
/// recognize is left to the default handler (a 500 that does not expose the exception).
/// </summary>
public sealed class ProblemDetailsExceptionHandler : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
