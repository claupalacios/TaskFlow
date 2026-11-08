using TaskFlow.Application.Users;

namespace TaskFlow.Application.Auth;

#pragma warning disable CS9113 // RED stub: dependencies are used once the service is implemented.
public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    public Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
