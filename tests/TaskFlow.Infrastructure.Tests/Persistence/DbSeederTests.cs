using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using TaskFlow.Application.Auth;
using TaskFlow.Domain;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Security;

namespace TaskFlow.Infrastructure.Tests.Persistence;

public sealed class DbSeederTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteTestDatabase _db = new();
    private readonly PasswordHasher _hasher = new();
    private readonly FakeTimeProvider _clock = new(Now);

    public void Dispose() => _db.Dispose();

    private async Task SeedAsync()
    {
        await using var context = _db.CreateContext();
        await new DbSeeder(context, _hasher, _clock).SeedAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesEveryDemoUser()
    {
        await SeedAsync();

        await using var context = _db.CreateContext();
        var emails = await context.Users.Select(u => u.Email).OrderBy(e => e).ToListAsync();
        Assert.Equal(DbSeeder.DemoAccounts.Select(a => a.Email).Order(), emails);
    }

    [Fact]
    public async Task SeedAsync_DemoPasswordsVerifyAgainstStoredHashes()
    {
        await SeedAsync();

        await using var context = _db.CreateContext();
        foreach (var account in DbSeeder.DemoAccounts)
        {
            var user = await context.Users.SingleAsync(u => u.Email == account.Email);
            Assert.True(_hasher.Verify(user.PasswordHash, account.Password), account.Email);
        }
    }

    [Fact]
    public void DemoAccounts_ComplyWithThePasswordPolicy()
    {
        Assert.NotEmpty(DbSeeder.DemoAccounts);
        foreach (var account in DbSeeder.DemoAccounts)
        {
            PasswordPolicy.Validate(account.Password, User.NormalizeEmail(account.Email));
        }
    }

    [Fact]
    public async Task SeedAsync_GivesEveryDemoUserTheirOwnTasks()
    {
        await SeedAsync();

        await using var context = _db.CreateContext();
        var users = await context.Users.ToListAsync();
        foreach (var user in users)
        {
            Assert.True(await context.Tasks.AnyAsync(t => t.UserId == user.Id), $"{user.Email} has no tasks");
        }
    }

    [Fact]
    public async Task SeedAsync_CoversEveryTaskStatus()
    {
        await SeedAsync();

        await using var context = _db.CreateContext();
        var statuses = await context.Tasks.Select(t => t.Status).Distinct().ToListAsync();
        Assert.Equal(Enum.GetValues<TaskItemStatus>().Order(), statuses.Order());
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateData()
    {
        await SeedAsync();
        await SeedAsync();

        await using var context = _db.CreateContext();
        Assert.Equal(DbSeeder.DemoAccounts.Count, await context.Users.CountAsync());
        Assert.Equal(DbSeeder.DemoTaskCount, await context.Tasks.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_DatabaseWithRealUsers_AddsNothing()
    {
        await using (var context = _db.CreateContext())
        {
            context.Users.Add(User.Create("real@example.com", _hasher.Hash("real-pass-1"), Now));
            await context.SaveChangesAsync();
        }

        await SeedAsync();

        await using var check = _db.CreateContext();
        Assert.Equal("real@example.com", (await check.Users.SingleAsync()).Email);
        Assert.Empty(await check.Tasks.ToListAsync());
    }
}
