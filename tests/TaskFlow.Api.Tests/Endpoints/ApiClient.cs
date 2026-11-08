using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Application.Auth;
using TaskFlow.Domain;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Api.Tests.Endpoints;

internal static class ApiClient
{
    public const string Password = "s3cure-passphrase-1";

    /// <summary>The API's wire format: camelCase, enums as strings.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string password = Password)
        => client.PostAsJsonAsync("/api/auth/register", new { email, password }, Json);

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password)
        => client.PostAsJsonAsync("/api/auth/login", new { email, password }, Json);

    /// <summary>Registers a fresh user, logs in, and returns a client that sends their bearer token.</summary>
    public static async Task<HttpClient> CreateUserClientAsync(TaskFlowApiFactory factory)
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();

        (await RegisterAsync(client, email)).EnsureSuccessStatusCode();
        var login = await LoginAsync(client, email);
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(Json);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    public static HttpClient WithBearer(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>A token built outside the API, to probe how it validates tokens.</summary>
    public static string ForgeToken(
        string signingKey = TaskFlowApiFactory.SigningKey,
        string audience = TaskFlowApiFactory.Audience,
        DateTimeOffset? issuedAt = null)
    {
        var options = new JwtOptions
        {
            Issuer = TaskFlowApiFactory.Issuer,
            Audience = audience,
            SigningKey = signingKey,
            ExpirationMinutes = 60,
        };
        var clock = new FakeTimeProvider(issuedAt ?? DateTimeOffset.UtcNow);
        var user = User.Create(UniqueEmail(), "unused-hash", clock.GetUtcNow());

        return new JwtTokenService(options, clock).CreateToken(user).Value;
    }
}
