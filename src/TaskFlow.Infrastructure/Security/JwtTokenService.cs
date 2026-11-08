using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.Auth;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;
    private readonly JsonWebTokenHandler _handler = new();

    public JwtTokenService(JwtOptions options, TimeProvider timeProvider)
    {
        // Fail at startup on a weak or missing configuration instead of issuing weak tokens.
        var keyBytes = Encoding.UTF8.GetBytes(options.SigningKey ?? string.Empty);
        if (keyBytes.Length < JwtOptions.MinSigningKeyBytes)
        {
            throw new ArgumentException(
                $"The JWT signing key must be at least {JwtOptions.MinSigningKeyBytes} bytes (256 bits) for HS256.", nameof(options));
        }

        if (options.ExpirationMinutes <= 0)
        {
            throw new ArgumentException("The JWT expiration must be a positive number of minutes.", nameof(options));
        }

        _options = options;
        _timeProvider = timeProvider;
        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateToken(User user)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.ExpirationMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
            },
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _signingCredentials,
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }
}
