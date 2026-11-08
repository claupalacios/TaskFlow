using TaskFlow.Application.Auth;

namespace TaskFlow.Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => throw new NotImplementedException();

    public bool Verify(string passwordHash, string password)
        => throw new NotImplementedException();
}
