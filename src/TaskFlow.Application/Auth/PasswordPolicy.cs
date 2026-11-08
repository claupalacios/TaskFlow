namespace TaskFlow.Application.Auth;

/// <summary>
/// Rules a new password must meet. Applied on registration only, never on login.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>Throws <see cref="Common.ValidationException"/> when the password breaks a rule.</summary>
    public static void Validate(string? password, string normalizedEmail)
        => throw new NotImplementedException();
}
