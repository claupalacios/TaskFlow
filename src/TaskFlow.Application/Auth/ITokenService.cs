using TaskFlow.Domain;

namespace TaskFlow.Application.Auth;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateToken(User user);
}
