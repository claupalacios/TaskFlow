using TaskFlow.Application.Common;
using TaskFlow.Domain;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Tests.Persistence;

public sealed class UserRepositoryTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly SqliteTestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private UserRepository NewRepository() => new(_db.CreateContext());

    // ---- AddAsync / GetByIdAsync ----

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAllFields()
    {
        var user = User.Create("ana@example.com", "hashed-password", Now);

        await NewRepository().AddAsync(user, Ct);
        var loaded = await NewRepository().GetByIdAsync(user.Id, Ct);

        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded.Id);
        Assert.Equal("ana@example.com", loaded.Email);
        Assert.Equal("hashed-password", loaded.PasswordHash);
        Assert.Equal(Now, loaded.CreatedAt);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var loaded = await NewRepository().GetByIdAsync(Guid.NewGuid(), Ct);

        Assert.Null(loaded);
    }

    // ---- GetByEmailAsync ----

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        var user = User.Create("ana@example.com", "hash", Now);
        await NewRepository().AddAsync(user, Ct);

        var loaded = await NewRepository().GetByEmailAsync("ana@example.com", Ct);

        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_IgnoresCaseAndSurroundingWhitespace()
    {
        var user = User.Create("ana@example.com", "hash", Now);
        await NewRepository().AddAsync(user, Ct);

        var loaded = await NewRepository().GetByEmailAsync("  ANA@Example.com ", Ct);

        Assert.NotNull(loaded);
        Assert.Equal(user.Id, loaded.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        await NewRepository().AddAsync(User.Create("ana@example.com", "hash", Now), Ct);

        var loaded = await NewRepository().GetByEmailAsync("bob@example.com", Ct);

        Assert.Null(loaded);
    }

    // ---- Integrity ----

    [Fact]
    public async Task AddAsync_DuplicateEmail_ThrowsConflictException()
    {
        // AuthService checks for the email first, but two concurrent registrations can both
        // pass that check. The unique index decides, and the repository must report it as a
        // conflict (409), not leak an EF exception that would surface as a 500.
        await NewRepository().AddAsync(User.Create("ana@example.com", "hash", Now), Ct);
        var duplicate = User.Create("ANA@example.com", "other-hash", Now);

        await Assert.ThrowsAsync<ConflictException>(() => NewRepository().AddAsync(duplicate, Ct));
    }
}
