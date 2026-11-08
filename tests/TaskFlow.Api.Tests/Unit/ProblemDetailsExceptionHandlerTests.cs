using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Errors;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Common;
using TaskFlow.Domain;

namespace TaskFlow.Api.Tests.Unit;

public class ProblemDetailsExceptionHandlerTests
{
    private readonly ProblemDetailsExceptionHandler _handler = new();

    private static DefaultHttpContext NewContext() => new() { Response = { Body = new MemoryStream() } };

    private static async Task<ProblemDetails> ReadProblemAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, JsonSerializerOptions.Web);
        Assert.NotNull(problem);
        return problem;
    }

    public static TheoryData<Exception, int> MappedExceptions => new()
    {
        { new DomainValidationException("Title is required."), StatusCodes.Status400BadRequest },
        { new ValidationException("Password is required."), StatusCodes.Status400BadRequest },
        { new InvalidCredentialsException(), StatusCodes.Status401Unauthorized },
        { new NotFoundException("Task 'x' was not found."), StatusCodes.Status404NotFound },
        { new ConflictException("An account with this email already exists."), StatusCodes.Status409Conflict },
    };

    [Theory]
    [MemberData(nameof(MappedExceptions))]
    public async Task TryHandleAsync_KnownException_WritesProblemDetailsWithItsStatusAndMessage(Exception exception, int status)
    {
        var context = NewContext();

        var handled = await _handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(status, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        var problem = await ReadProblemAsync(context);
        Assert.Equal(status, problem.Status);
        Assert.Equal(exception.Message, problem.Detail);
        Assert.False(string.IsNullOrEmpty(problem.Title));
    }

    [Fact]
    public async Task TryHandleAsync_UnknownException_IsLeftToTheDefaultHandler()
    {
        var context = NewContext();

        var handled = await _handler.TryHandleAsync(context, new InvalidOperationException("db password is hunter2"), CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(0, context.Response.Body.Length);
    }
}
