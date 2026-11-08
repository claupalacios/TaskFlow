using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Tasks;
using TaskFlow.Domain;

namespace TaskFlow.Api.Tests.Endpoints;

public sealed class TasksEndpointsTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private static async Task<TaskResponse> CreateTaskAsync(HttpClient client, string title = "Write report")
    {
        var response = await client.PostAsJsonAsync(
            "/api/tasks", new { title, description = "Q4 numbers", dueDate = "2026-11-20" }, ApiClient.Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(ApiClient.Json))!;
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)status, (await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiClient.Json))?.Status);
    }

    // ---- Unauthorized access ----

    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("GET", "/api/tasks/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("PUT", "/api/tasks/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    [InlineData("DELETE", "/api/tasks/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    public async Task AnyTaskEndpoint_WithoutToken_Returns401(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = JsonContent.Create(new { title = "x", status = "Pending" }),
        };

        var response = await factory.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- CRUD ----

    [Fact]
    public async Task Create_Returns201WithALocationThatResolvesToTheTask()
    {
        var client = await ApiClient.CreateUserClientAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/api/tasks", new { title = "Write report", description = "Q4 numbers", dueDate = "2026-11-20" }, ApiClient.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TaskResponse>(ApiClient.Json);
        Assert.Equal("Write report", created?.Title);
        Assert.Equal(TaskItemStatus.Pending, created?.Status);
        Assert.Equal(new DateOnly(2026, 11, 20), created?.DueDate);

        var fetched = await client.GetFromJsonAsync<TaskResponse>(response.Headers.Location, ApiClient.Json);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Create_SerializesStatusAsText()
    {
        var client = await ApiClient.CreateUserClientAsync(factory);
        await CreateTaskAsync(client);

        var json = await client.GetStringAsync("/api/tasks");

        Assert.Contains("\"status\":\"Pending\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_BlankTitle_Returns400()
    {
        var client = await ApiClient.CreateUserClientAsync(factory);

        var response = await client.PostAsJsonAsync("/api/tasks", new { title = "   " }, ApiClient.Json);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_ReturnsOnlyTheCallersTasks()
    {
        var ana = await ApiClient.CreateUserClientAsync(factory);
        var bruno = await ApiClient.CreateUserClientAsync(factory);
        var anasTask = await CreateTaskAsync(ana, "Ana's task");
        await CreateTaskAsync(bruno, "Bruno's task");

        var tasks = await ana.GetFromJsonAsync<List<TaskResponse>>("/api/tasks", ApiClient.Json);

        Assert.Equal(anasTask.Id, Assert.Single(tasks!).Id);
    }

    [Fact]
    public async Task Update_Returns200AndPersistsTheChanges()
    {
        var client = await ApiClient.CreateUserClientAsync(factory);
        var task = await CreateTaskAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/tasks/{task.Id}", new { title = "Final report", description = "Done", status = "Completed", dueDate = (string?)null }, ApiClient.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await client.GetFromJsonAsync<TaskResponse>($"/api/tasks/{task.Id}", ApiClient.Json);
        Assert.Equal("Final report", fetched?.Title);
        Assert.Equal(TaskItemStatus.Completed, fetched?.Status);
        Assert.Null(fetched?.DueDate);
    }

    [Fact]
    public async Task Update_UnknownStatus_Returns400()
    {
        var client = await ApiClient.CreateUserClientAsync(factory);
        var task = await CreateTaskAsync(client);

        var response = await client.PutAsJsonAsync($"/api/tasks/{task.Id}", new { title = "t", status = "Archived" }, ApiClient.Json);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Returns204AndTheTaskIsGone()
    {
        var client = await ApiClient.CreateUserClientAsync(factory);
        var task = await CreateTaskAsync(client);

        var response = await client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await AssertProblemAsync(await client.GetAsync($"/api/tasks/{task.Id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        var client = await ApiClient.CreateUserClientAsync(factory);

        await AssertProblemAsync(await client.GetAsync($"/api/tasks/{Guid.NewGuid()}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_IdThatIsNotAGuid_Returns404()
    {
        var client = await ApiClient.CreateUserClientAsync(factory);

        var response = await client.GetAsync("/api/tasks/123");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Ownership ----

    [Fact]
    public async Task AnotherUsersTask_GetUpdateAndDeleteReturn404AndLeaveItUntouched()
    {
        var owner = await ApiClient.CreateUserClientAsync(factory);
        var intruder = await ApiClient.CreateUserClientAsync(factory);
        var task = await CreateTaskAsync(owner);
        var url = $"/api/tasks/{task.Id}";

        await AssertProblemAsync(await intruder.GetAsync(url), HttpStatusCode.NotFound);
        await AssertProblemAsync(
            await intruder.PutAsJsonAsync(url, new { title = "Hijacked", status = "Completed" }, ApiClient.Json),
            HttpStatusCode.NotFound);
        await AssertProblemAsync(await intruder.DeleteAsync(url), HttpStatusCode.NotFound);

        var stillThere = await owner.GetFromJsonAsync<TaskResponse>(url, ApiClient.Json);
        Assert.Equal(task, stillThere);
    }

    [Fact]
    public async Task Create_UserIdInTheBodyIsIgnored()
    {
        var caller = await ApiClient.CreateUserClientAsync(factory);
        var victim = await ApiClient.CreateUserClientAsync(factory);
        var victimId = (await victim.GetFromJsonAsync<Application.Auth.UserResponse>("/api/users/me", ApiClient.Json))!.Id;

        var response = await caller.PostAsJsonAsync("/api/tasks", new { title = "Planted", userId = victimId }, ApiClient.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Empty((await victim.GetFromJsonAsync<List<TaskResponse>>("/api/tasks", ApiClient.Json))!);
        Assert.Single((await caller.GetFromJsonAsync<List<TaskResponse>>("/api/tasks", ApiClient.Json))!);
    }
}
