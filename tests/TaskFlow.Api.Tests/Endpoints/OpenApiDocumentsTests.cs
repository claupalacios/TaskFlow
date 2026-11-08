using System.Net;
using System.Text.Json;

namespace TaskFlow.Api.Tests.Endpoints;

/// <summary>
/// The host exposes two APIs (tasks, identity), each described by its own OpenAPI document.
/// </summary>
public sealed class OpenApiDocumentsTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private async Task<IReadOnlyList<string>> PathsOfAsync(string document)
    {
        var response = await factory.CreateClient().GetAsync($"/openapi/{document}.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
    }

    [Fact]
    public async Task TasksDocument_DescribesOnlyTheTaskEndpoints()
    {
        var paths = await PathsOfAsync("tasks");

        Assert.Equal(["/api/tasks", "/api/tasks/{id}"], paths.Order());
    }

    [Fact]
    public async Task IdentityDocument_DescribesOnlyTheIdentityEndpoints()
    {
        var paths = await PathsOfAsync("identity");

        Assert.Equal(["/api/auth/login", "/api/auth/register", "/api/users/me"], paths.Order());
    }

    [Theory]
    [InlineData("tasks", "/api/tasks", "get", true)]
    [InlineData("tasks", "/api/tasks/{id}", "delete", true)]
    [InlineData("identity", "/api/users/me", "get", true)]
    [InlineData("identity", "/api/auth/login", "post", false)]
    [InlineData("identity", "/api/auth/register", "post", false)]
    public async Task Documents_MarkOnlyAuthorizedOperationsWithTheBearerScheme(
        string document, string path, string method, bool requiresBearer)
    {
        var response = await factory.CreateClient().GetAsync($"/openapi/{document}.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("bearer", root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer").GetProperty("scheme").GetString());
        var operation = root.GetProperty("paths").GetProperty(path).GetProperty(method);
        var hasBearer = operation.TryGetProperty("security", out var security)
                        && security.EnumerateArray().Any(r => r.TryGetProperty("Bearer", out _));
        Assert.Equal(requiresBearer, hasBearer);
    }
}
