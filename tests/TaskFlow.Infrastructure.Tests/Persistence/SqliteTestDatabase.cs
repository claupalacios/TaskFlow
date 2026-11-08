using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Tests.Persistence;

/// <summary>
/// A private in-memory SQLite database per test, built by applying the migrations. The connection stays open for the
/// test's lifetime (closing it drops the database). Each <see cref="CreateContext"/>
/// call returns a fresh DbContext, so assertions read from the database rather than
/// from a change tracker that already holds the entities.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        // Migrate rather than EnsureCreated, so every persistence test runs against the
        // schema the committed migrations produce, the same one a real database gets.
        using var context = CreateContext();
        context.Database.Migrate();
    }

    public AppDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
