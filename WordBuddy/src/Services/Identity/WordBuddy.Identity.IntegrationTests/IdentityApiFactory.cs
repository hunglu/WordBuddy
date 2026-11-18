using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Api;
using WordBuddy.Identity.Infrastructure.Persistence;

namespace WordBuddy.Identity.IntegrationTests;

/// <summary>
/// Boots the real Identity API host against a per-run database (migrated and seeded by
/// the Development startup, dropped on disposal). Provider: SQL Server (default) or PostgreSQL —
/// set <c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c> and <c>WORDBUDDY_TEST_PG_CONNECTION_STRING</c> (server part only,
/// e.g. <c>Host=localhost;Username=postgres;Password=...</c>; the factory adds the database name). In-memory MassTransit transport with the test
/// harness. The unlink wait is 0 days so escalation can be tested end to end (the wait itself is
/// unit tested). Override the SQL Server database with <c>IDENTITY_TEST_CONNECTION_STRING</c>.
/// Configuration goes through environment variables, as in the Progress and Content factories.
/// </summary>
public sealed class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecret = "integration-test-secret-do-not-use-in-production-32chars";
    public const string JwtIssuer = "WordBuddy";

    /// <summary>Development seed admin (see <c>DataSeeder</c>).</summary>
    public const string AdminEmail = "admin@wordbuddy.com";
    public const string AdminPassword = "Admin@123";

    /// <summary>True when the run targets PostgreSQL (<c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c>).</summary>
    public static readonly bool IsPostgres = string.Equals(
        Environment.GetEnvironmentVariable("WORDBUDDY_TEST_DB_PROVIDER"), "Postgres", StringComparison.OrdinalIgnoreCase);

    private static readonly string ConnectionString = BuildConnectionString();

    private static string BuildConnectionString()
    {
        string databaseName = $"WordBuddyIdentityTests_{Guid.NewGuid():N}";
        if (IsPostgres)
        {
            string server = Environment.GetEnvironmentVariable("WORDBUDDY_TEST_PG_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Set WORDBUDDY_TEST_PG_CONNECTION_STRING (server part only) when WORDBUDDY_TEST_DB_PROVIDER=Postgres.");
            return $"{server.TrimEnd(';')};Database={databaseName}";
        }

        return Environment.GetEnvironmentVariable("IDENTITY_TEST_CONNECTION_STRING")
            ?? $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=true";
    }

    static IdentityApiFactory()
    {
        Environment.SetEnvironmentVariable("Database__Provider", IsPostgres ? "Postgres" : "SqlServer");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Messaging__Transport", "InMemory");
        Environment.SetEnvironmentVariable("SupportLinks__UnlinkOverrideWaitDays", "0");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    public Task InitializeAsync()
    {
        // Building the host runs the Development migrate + seed.
        _ = Services;
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }
}

/// <summary>All Identity API test classes share one factory (one database per run).</summary>
[CollectionDefinition(Name)]
public sealed class IdentityApiCollection : ICollectionFixture<IdentityApiFactory>
{
    public const string Name = "IdentityApi";
}
