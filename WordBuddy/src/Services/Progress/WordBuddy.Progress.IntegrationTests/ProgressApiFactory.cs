using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Api;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Infrastructure.ContentClient;
using WordBuddy.Progress.Infrastructure.Persistence;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// Boots the real Progress API host against a dedicated, per-run database (migrated on
/// startup, dropped on disposal) — never mocks EF Core, per repo convention. Default: local SQL Server
/// (override with <c>PROGRESS_TEST_CONNECTION_STRING</c>). Set <c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c> and
/// <c>WORDBUDDY_TEST_PG_CONNECTION_STRING</c> (server part only, e.g. <c>Host=localhost;Username=postgres;Password=...</c>)
/// to run on PostgreSQL.
///
/// Configuration is injected via process environment variables, not
/// <c>WebApplicationFactory.ConfigureWebHost</c>'s <c>ConfigureAppConfiguration</c> — see
/// Content's <c>ContentApiFactory</c> for the full explanation of why (Progress.Api's
/// <c>Program.Main</c> has the identical minimal-hosting shape and reads <c>builder.Configuration</c>
/// synchronously before that hook would ever run).
///
/// Content is called over REST here (<c>Services:Content:Transport=Rest</c>, WB-68), through the stub handler.
/// </summary>
public sealed class ProgressApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecret = "integration-test-secret-do-not-use-in-production-32chars";
    public const string JwtIssuer = "WordBuddy";

    /// <summary>True when the run targets PostgreSQL (<c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c>).</summary>
    public static readonly bool IsPostgres = string.Equals(
        Environment.GetEnvironmentVariable("WORDBUDDY_TEST_DB_PROVIDER"), "Postgres", StringComparison.OrdinalIgnoreCase);

    private static readonly string ConnectionString = BuildConnectionString();

    private static string BuildConnectionString()
    {
        string databaseName = $"WordBuddyProgressTests_{Guid.NewGuid():N}";
        if (IsPostgres)
        {
            string server = Environment.GetEnvironmentVariable("WORDBUDDY_TEST_PG_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Set WORDBUDDY_TEST_PG_CONNECTION_STRING (server part only) when WORDBUDDY_TEST_DB_PROVIDER=Postgres.");
            return $"{server.TrimEnd(';')};Database={databaseName}";
        }

        return Environment.GetEnvironmentVariable("PROGRESS_TEST_CONNECTION_STRING")
            ?? $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=true";
    }

    static ProgressApiFactory()
    {
        Environment.SetEnvironmentVariable("Database__Provider", IsPostgres ? "Postgres" : "SqlServer");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Messaging__Transport", "InMemory");

        // WB-68: gRPC is the default transport. This factory pins REST so the existing suite keeps proving
        // that the REST alternative works; ProgressGrpcApiFactory covers the default.
        Environment.SetEnvironmentVariable("Services__Content__Transport", "Rest");

        // Tests drive the challenge life cycle and score projection by hand; the background clock stays off.
        Environment.SetEnvironmentVariable("Challenges__SchedulerEnabled", "false");
    }

    /// <summary>Stand-in for the Content service; tests register the senses it returns.</summary>
    public StubContentHandler Content { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // The typed client of IContentSenseClient is named after the interface. Its primary handler is
        // replaced by the stub; the real ContentSenseClient, token forwarding and cache still run.
        // The readiness check calls Content's /health through its own named client; the stub answers 200.
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient(nameof(IContentSenseClient)).ConfigurePrimaryHttpMessageHandler(() => Content);
            services.AddHttpClient(ContentHealthCheck.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => Content);
        });
    }

    public async Task InitializeAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }
}
