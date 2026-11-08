using TaskFlow.Domain;

namespace TaskFlow.Application.Users;

/// <summary>
/// Persistence port for users. Email lookups are normalized, so callers may pass the
/// address as typed. Each write persists immediately.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
