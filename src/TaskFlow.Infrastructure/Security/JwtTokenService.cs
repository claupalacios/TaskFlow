using TaskFlow.Application.Auth;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Security;

#pragma warning disable CS9113 // RED stub: dependencies are used once the service is implemented.
public sealed class JwtTokenService(JwtOptions options, TimeProvider timeProvider) : ITokenService
{
    public AccessToken CreateToken(User user)
        => throw new NotImplementedException();
}
