namespace TaskFlow.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 needs a key of at least 256 bits (32 bytes of UTF-8).</summary>
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
