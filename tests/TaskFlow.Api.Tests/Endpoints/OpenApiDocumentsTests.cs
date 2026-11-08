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
}
