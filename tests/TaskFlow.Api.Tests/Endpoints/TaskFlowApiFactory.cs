using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace TaskFlow.Api.Tests.Endpoints;

/// <summary>
/// Runs the real API in memory against its own temporary SQLite file, with JWT settings
/// known to the tests so they can also forge invalid tokens.
/// </summary>
public class TaskFlowApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "TaskFlow.Tests";
    public const string Audience = "TaskFlow.Tests.Client";
    public const string SigningKey = "endpoint-tests-signing-key-with-32-bytes-or-more";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"taskflow-tests-{Guid.NewGuid():N}.db");

    protected virtual bool SeedDemoData => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TaskFlow"] = $"Data Source={_databasePath}",
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:SigningKey"] = SigningKey,
            ["Jwt:ExpirationMinutes"] = "60",
            ["Database:MigrateOnStartup"] = "true",
            ["Database:SeedDemoData"] = SeedDemoData.ToString(),
        }));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}

/// <summary>Same API with the demo data seeded at startup, as in Development.</summary>
public sealed class SeededTaskFlowApiFactory : TaskFlowApiFactory
{
    protected override bool SeedDemoData => true;
}
