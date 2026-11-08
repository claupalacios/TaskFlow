namespace TaskFlow.Application.Auth;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>Returns false (never throws) when the hash is malformed or does not match.</summary>
    bool Verify(string passwordHash, string password);
}
