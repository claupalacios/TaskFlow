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
        => throw new NotImplementedException();

    /// <summary>Canonical form used for storage and lookups.</summary>
    public static string NormalizeEmail(string? email)
        => throw new NotImplementedException();
}
