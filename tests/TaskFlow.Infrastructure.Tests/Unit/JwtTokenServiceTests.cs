using System.Text;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Domain;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Infrastructure.Tests.Unit;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private const string SigningKey = "test-signing-key-with-at-least-32-bytes!";

    private readonly FakeTimeProvider _clock = new(Now);
    private readonly User _user = User.Create("ana@example.com", "stored-password-hash", Now);

    private static JwtOptions Options(string signingKey = SigningKey, int expirationMinutes = 60) => new()
    {
        Issuer = "TaskFlow",
        Audience = "TaskFlow.Client",
        SigningKey = signingKey,
        ExpirationMinutes = expirationMinutes,
    };

    private string CreateToken() => new JwtTokenService(Options(), _clock).CreateToken(_user).Value;

    private static Task<TokenValidationResult> ValidateAsync(string token, string signingKey = SigningKey)
        => new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = "TaskFlow",
            ValidAudience = "TaskFlow.Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateLifetime = false, // lifetime is asserted explicitly against the fake clock
        });

    // ---- Content ----

    [Fact]
    public void CreateToken_IdentifiesTheUserBySubjectAndEmail()
    {
        var jwt = new JsonWebToken(CreateToken());

        Assert.Equal(_user.Id.ToString(), jwt.Subject);
        Assert.Equal("ana@example.com", jwt.GetClaim(JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public void CreateToken_HasConfiguredIssuerAndAudience()
    {
        var jwt = new JsonWebToken(CreateToken());

        Assert.Equal("TaskFlow", jwt.Issuer);
        Assert.Equal(["TaskFlow.Client"], jwt.Audiences);
    }

    [Fact]
    public void CreateToken_DoesNotLeakThePasswordHash()
    {
        var jwt = new JsonWebToken(CreateToken());

        Assert.DoesNotContain(jwt.Claims, c => c.Value.Contains("stored-password-hash", StringComparison.Ordinal));
    }

    // ---- Lifetime ----

    [Fact]
    public void CreateToken_ExpiresAfterTheConfiguredMinutes()
    {
        var accessToken = new JwtTokenService(Options(expirationMinutes: 60), _clock).CreateToken(_user);
        var jwt = new JsonWebToken(accessToken.Value);

        Assert.Equal(Now.AddMinutes(60), accessToken.ExpiresAt);
        Assert.Equal(Now.AddMinutes(60).UtcDateTime, jwt.ValidTo);
        Assert.Equal(Now.UtcDateTime, jwt.IssuedAt);
        Assert.Equal(Now.UtcDateTime, jwt.ValidFrom);
    }

    // ---- Signature ----

    [Fact]
    public void CreateToken_IsSignedWithHs256()
    {
        var jwt = new JsonWebToken(CreateToken());

        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Alg);
    }

    [Fact]
    public async Task CreateToken_ValidatesWithTheConfiguredKey()
    {
        var result = await ValidateAsync(CreateToken());

        Assert.True(result.IsValid, result.Exception?.Message);
    }

    [Fact]
    public async Task CreateToken_FailsValidationWithAnotherKey()
    {
        var result = await ValidateAsync(CreateToken(), "another-signing-key-with-32-bytes-or-more");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CreateToken_WithTamperedPayload_FailsValidation()
    {
        var parts = CreateToken().Split('.');
        var forgedPayload = Base64UrlEncoder.Encode($$"""{"sub":"{{Guid.NewGuid()}}","iss":"TaskFlow","aud":"TaskFlow.Client"}""");

        var result = await ValidateAsync($"{parts[0]}.{forgedPayload}.{parts[2]}");

        Assert.False(result.IsValid);
    }

    // ---- Configuration guards ----

    [Theory]
    [InlineData("")]
    [InlineData("too-short-key")]
    [InlineData("exactly-31-bytes-long-key-12345")]
    public void Constructor_WithSigningKeyShorterThan256Bits_Throws(string signingKey)
    {
        Assert.Throws<ArgumentException>(() => new JwtTokenService(Options(signingKey), _clock));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_WithNonPositiveExpiration_Throws(int expirationMinutes)
    {
        Assert.Throws<ArgumentException>(() => new JwtTokenService(Options(expirationMinutes: expirationMinutes), _clock));
    }
}
