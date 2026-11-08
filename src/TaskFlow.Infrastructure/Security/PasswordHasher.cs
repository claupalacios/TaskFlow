using Microsoft.AspNetCore.Identity;
using TaskFlow.Application.Auth;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Security;

/// <summary>
/// Delegates to ASP.NET Core Identity's hasher (PBKDF2 with a random salt per hash), so
/// no cryptography is hand-written here.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    // Identity's default hasher ignores the user argument, so none is passed.
    public string Hash(string password) => _inner.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password)
    {
        try
        {
            return _inner.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            // Not valid Base64: treat as a mismatch rather than letting a corrupt row crash login.
            return false;
        }
    }
}
