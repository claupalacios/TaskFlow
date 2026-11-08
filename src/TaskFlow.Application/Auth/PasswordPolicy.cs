using TaskFlow.Application.Common;

namespace TaskFlow.Application.Auth;

/// <summary>
/// Rules a new password must meet. Applied on registration only, never on login.
/// Messages describe the rule and never include the password itself.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>Throws <see cref="ValidationException"/> when the password breaks a rule.</summary>
    public static void Validate(string? password, string normalizedEmail)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ValidationException("Password is required.");
        }

        if (password.Length is < MinLength or > MaxLength)
        {
            throw new ValidationException($"Password must be between {MinLength} and {MaxLength} characters.");
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new ValidationException("Password must contain at least one letter and one digit.");
        }

        if (string.Equals(password, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Password must not be the same as the email.");
        }
    }
}
