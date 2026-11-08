using TaskFlow.Application.Users;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence;

#pragma warning disable CS9113 // RED stub: dbContext is used once the repository is implemented.
public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public Task AddAsync(User user, CancellationToken cancellationToken)
        => throw new NotImplementedException();
}
