using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Api.Tests.Endpoints;

public sealed class IdentityEndpointsTests(TaskFlowApiFactory factory) : IClassFixture<TaskFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiClient.Json);
        Assert.Equal((int)status, problem?.Status);
    }

    // ---- Register ----

    [Fact]
    public async Task Register_ValidRequest_Returns201WithProfileAndLocation()
    {
        var email = ApiClient.UniqueEmail();

        var response = await ApiClient.RegisterAsync(_client, email);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/users/me", response.Headers.Location?.AbsolutePath);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>(ApiClient.Json);
        Assert.Equal(email, user?.Email);
        Assert.NotEqual(Guid.Empty, user?.Id);
    }

    [Fact]
    public async Task Register_ResponseNeverContainsPasswordData()
    {
        var response = await ApiClient.RegisterAsync(_client, ApiClient.UniqueEmail());

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ApiClient.Password, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_DuplicateEmailInAnyCase_Returns409()
    {
        var email = ApiClient.UniqueEmail();
        await ApiClient.RegisterAsync(_client, email);

        var response = await ApiClient.RegisterAsync(_client, email.ToUpperInvariant());

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("onlyletters")]
    [InlineData("12345678")]
    public async Task Register_PasswordBreakingPolicy_Returns400(string password)
    {
        var response = await ApiClient.RegisterAsync(_client, ApiClient.UniqueEmail(), password);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var response = await ApiClient.RegisterAsync(_client, "not-an-email");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_MalformedJson_Returns400()
    {
        var response = await _client.PostAsync(
            "/api/auth/register", new StringContent("{ not json", Encoding.UTF8, "application/json"));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    // ---- Login ----

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var email = ApiClient.UniqueEmail();
        await ApiClient.RegisterAsync(_client, email);

        var response = await ApiClient.LoginAsync(_client, email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(ApiClient.Json);
        Assert.False(string.IsNullOrEmpty(auth?.AccessToken));
        Assert.True(auth!.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownEmail_Return401WithTheSameProblem()
    {
        var email = ApiClient.UniqueEmail();
        await ApiClient.RegisterAsync(_client, email);

        var wrongPassword = await ApiClient.LoginAsync(_client, email, "wrong-password-1");
        var unknownEmail = await ApiClient.LoginAsync(_client, ApiClient.UniqueEmail());

        await AssertProblemAsync(wrongPassword, HttpStatusCode.Unauthorized);
        await AssertProblemAsync(unknownEmail, HttpStatusCode.Unauthorized);
        Assert.Equal(await wrongPassword.Content.ReadAsStringAsync(), await unknownEmail.Content.ReadAsStringAsync());
    }

    // ---- Current user (authorized endpoint) ----

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsTheCallersProfile()
    {
        var email = ApiClient.UniqueEmail();
        await ApiClient.RegisterAsync(_client, email);
        var auth = await (await ApiClient.LoginAsync(_client, email)).Content.ReadFromJsonAsync<AuthResponse>(ApiClient.Json);

        var response = await factory.CreateClient().WithBearer(auth!.AccessToken).GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(email, (await response.Content.ReadFromJsonAsync<UserResponse>(ApiClient.Json))?.Email);
    }

    [Fact]
    public async Task Me_WithExpiredToken_Returns401()
    {
        var expired = ApiClient.ForgeToken(issuedAt: DateTimeOffset.UtcNow.AddHours(-2));

        var response = await factory.CreateClient().WithBearer(expired).GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTokenSignedByAnotherKey_Returns401()
    {
        var forged = ApiClient.ForgeToken(signingKey: "attacker-signing-key-with-32-bytes-or-more");

        var response = await factory.CreateClient().WithBearer(forged).GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTokenForAnotherAudience_Returns401()
    {
        var foreign = ApiClient.ForgeToken(audience: "SomeOtherApp");

        var response = await factory.CreateClient().WithBearer(foreign).GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithTamperedSignature_Returns401()
    {
        var token = ApiClient.ForgeToken();
        var tampered = token[..^4] + (token.EndsWith("AAAA", StringComparison.Ordinal) ? "BBBB" : "AAAA");

        var response = await factory.CreateClient().WithBearer(tampered).GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public static TheoryData<string, string?> TokensWithoutAValidSubject => new()
    {
        { "/api/users/me", null },
        { "/api/users/me", "" },
        { "/api/users/me", "not-a-guid" },
        { "/api/tasks", null },
        { "/api/tasks", "not-a-guid" },
    };

    [Theory]
    [MemberData(nameof(TokensWithoutAValidSubject))]
    public async Task AuthorizedEndpoint_SignedTokenWithoutAValidSubject_Returns401(string url, string? subject)
    {
        // Signed with the right key, but the API cannot tell whose request it is.
        // It must be rejected as unauthenticated, never surface as a 500.
        var claims = new Dictionary<string, object> { ["email"] = "ana@example.com" };
        if (subject is not null)
        {
            claims["sub"] = subject;
        }

        var response = await factory.CreateClient().WithBearer(ApiClient.SignToken(claims)).GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ValidTokenForUserThatDoesNotExist_Returns404()
    {
        // Correctly signed, but its subject was never registered (for example, a deleted account).
        var response = await factory.CreateClient().WithBearer(ApiClient.ForgeToken()).GetAsync("/api/users/me");

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}

public sealed class DemoDataEndpointsTests(SeededTaskFlowApiFactory factory) : IClassFixture<SeededTaskFlowApiFactory>
{
    [Fact]
    public async Task Login_WithEveryPublishedDemoAccount_Succeeds()
    {
        var client = factory.CreateClient();

        foreach (var account in DbSeeder.DemoAccounts)
        {
            var response = await ApiClient.LoginAsync(client, account.Email, account.Password);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{account.Email}: {(int)response.StatusCode}");
        }
    }
}
