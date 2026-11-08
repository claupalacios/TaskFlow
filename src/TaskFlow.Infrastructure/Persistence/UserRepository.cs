using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Users;
using TaskFlow.Domain;

namespace TaskFlow.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
        => dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);

        return dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraint })
        {
            // Two registrations raced past AuthService's check; the unique index on Email decided.
            dbContext.Entry(user).State = EntityState.Detached;
            throw new ConflictException("An account with this email already exists.");
        }
    }

    // SQLITE_CONSTRAINT_UNIQUE. Email is the only unique column besides the primary key.
    private const int SqliteUniqueConstraint = 2067;
}
