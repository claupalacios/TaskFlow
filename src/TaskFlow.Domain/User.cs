namespace TaskFlow.Domain;

public sealed class User
{
    public const int EmailMaxLength = 256;

    private User()
    {
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public static User Create(string? email, string? passwordHash, DateTimeOffset now)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (normalizedEmail.Length == 0)
        {
            throw new DomainValidationException("Email is required.");
        }

        if (normalizedEmail.Length > EmailMaxLength)
        {
            throw new DomainValidationException($"Email cannot exceed {EmailMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainValidationException("A password hash is required.");
        }

        return new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            CreatedAt = now,
        };
    }

    /// <summary>Canonical form used for storage and lookups.</summary>
    public static string NormalizeEmail(string? email)
        => email?.Trim().ToLowerInvariant() ?? string.Empty;
}
