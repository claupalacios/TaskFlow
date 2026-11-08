using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Infrastructure.Tests.Persistence;

/// <summary>
/// The committed migrations are the real schema. These tests fail if someone changes the
/// model without adding a migration, or if the migrations cannot build a database from scratch.
/// </summary>
public sealed class MigrationsTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Model_HasNoChangesMissingFromMigrations()
    {
        using var context = _db.CreateContext();

        Assert.False(
            context.Database.HasPendingModelChanges(),
            "The EF model changed without a migration. Run: dotnet ef migrations add <Name>");
    }

    [Fact]
    public void Migrate_OnEmptyDatabase_AppliesEveryMigration()
    {
        using var context = _db.CreateContext();

        var defined = context.Database.GetMigrations().ToList();
        var applied = context.Database.GetAppliedMigrations().ToList();

        Assert.NotEmpty(defined);
        Assert.Equal(defined, applied);
    }
}
