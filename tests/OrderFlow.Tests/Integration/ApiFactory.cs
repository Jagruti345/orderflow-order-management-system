using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Persistence;
using Xunit;

namespace OrderFlow.Tests.Integration;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin#12345";

    // In CI (Phase 8) we pass the connection string through this environment variable
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Server=localhost,1433;Database=OrderFlowDb_Tests;User Id=sa;Password=OrderFlow#Dev2026;TrustServerCertificate=True";

    public ApiFactory()
    {
        // Safety net: this factory DELETES its database on every run
        if (!ConnectionString.Contains("Test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refusing to run integration tests against a database whose name does not contain 'Test'.");

        // Program.cs reads configuration before the host is built, so we use environment variables.
        // They override appsettings.json and user secrets.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Key", new string('t', 64));
        Environment.SetEnvironmentVariable("Seed__AdminEmail", AdminEmail);
        Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitPerMinute", "1000");
        Environment.SetEnvironmentVariable("RateLimiting__GlobalPermitPerMinute", "100000");
    }

    public async Task InitializeAsync()
    {
        // Build a fresh schema from our real migrations BEFORE the app starts,
        // because Program.cs seeds the admin user on startup and needs the tables to exist.
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options;
        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        _ = Services;   // touching Services starts the app
    }

    // xUnit's DisposeAsync returns Task, the factory's returns ValueTask, so we implement xUnit's explicitly.
    // The factory itself is still cleaned up through IDisposable.
    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFactory> { }