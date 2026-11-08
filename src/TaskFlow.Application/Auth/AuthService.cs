using System.Net.Mail;
using TaskFlow.Application.Common;
using TaskFlow.Application.Users;
using TaskFlow.Domain;

namespace TaskFlow.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    // Hash of a random password, verified when the email is unknown so that a failed login
    // costs the same whether or not the account exists. Computed once per process.
    private static string? s_timingDummyHash;

    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);

        if (!IsValidEmail(email))
        {
            throw new ValidationException("A valid email is required.");
        }

        PasswordPolicy.Validate(request.Password, email);

        if (await users.GetByEmailAsync(email, cancellationToken) is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = User.Create(email, passwordHasher.Hash(request.Password!), timeProvider.GetUtcNow());

        await users.AddAsync(user, cancellationToken);

        return ToResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = User.NormalizeEmail(request.Email);
        var password = request.Password ?? string.Empty;

        var user = email.Length == 0 ? null : await users.GetByEmailAsync(email, cancellationToken);

        var passwordHash = user?.PasswordHash ?? (s_timingDummyHash ??= passwordHasher.Hash(Guid.NewGuid().ToString()));
        var passwordMatches = passwordHasher.Verify(passwordHash, password);

        if (user is null || password.Length == 0 || !passwordMatches)
        {
            throw new InvalidCredentialsException();
        }

        var token = tokenService.CreateToken(user);

        return new AuthResponse(token.Value, token.ExpiresAt);
    }

    public async Task<UserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException("User was not found.");

        return ToResponse(user);
    }

    // A bare address only: MailAddress also accepts "Name <address>", which must not be stored as an email.
    private static bool IsValidEmail(string email)
        => email.Length > 0
           && MailAddress.TryCreate(email, out var address)
           && address.Address == email;

    private static UserResponse ToResponse(User user) => new(user.Id, user.Email, user.CreatedAt);
}
