using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Api;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>
/// Boots the real Content API host against a dedicated, per-run database (migrated on
/// startup, dropped on disposal) — never mocks EF Core, per repo convention. Default: local SQL Server
/// (override with <c>CONTENT_TEST_CONNECTION_STRING</c>). Set <c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c> and
/// <c>WORDBUDDY_TEST_PG_CONNECTION_STRING</c> (server part only, e.g. <c>Host=localhost;Username=postgres;Password=...</c>)
/// to run on PostgreSQL.
///
/// Configuration is injected via process environment variables, not
/// <c>WebApplicationFactory.ConfigureWebHost</c>'s <c>ConfigureAppConfiguration</c> — Content.Api's
/// <c>Program.Main</c> reads <c>builder.Configuration</c> synchronously (e.g. inside
/// <c>AddWordBuddyAuthentication</c>) before the deferred host-building pipeline that
/// <c>ConfigureWebHost</c> hooks into ever runs, so values added there arrive too late for a
/// minimal-hosting (<c>WebApplication.CreateBuilder</c>) entry point. Environment variables are
/// read at <c>WebApplication.CreateBuilder(args)</c> time instead, which is early enough — the
/// same mechanism docker-compose already uses to configure this service in every other
/// environment, so this is also the most faithful way to configure it here.
/// </summary>
public sealed class ContentApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecret = "integration-test-secret-do-not-use-in-production-32chars";
    public const string JwtIssuer = "WordBuddy";

    /// <summary>True when the run targets PostgreSQL (<c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c>).</summary>
    public static readonly bool IsPostgres = string.Equals(
        Environment.GetEnvironmentVariable("WORDBUDDY_TEST_DB_PROVIDER"), "Postgres", StringComparison.OrdinalIgnoreCase);

    private static readonly string ConnectionString = BuildConnectionString();

    private static string BuildConnectionString()
    {
        string databaseName = $"WordBuddyContentTests_{Guid.NewGuid():N}";
        if (IsPostgres)
        {
            string server = Environment.GetEnvironmentVariable("WORDBUDDY_TEST_PG_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Set WORDBUDDY_TEST_PG_CONNECTION_STRING (server part only) when WORDBUDDY_TEST_DB_PROVIDER=Postgres.");
            return $"{server.TrimEnd(';')};Database={databaseName}";
        }

        return Environment.GetEnvironmentVariable("CONTENT_TEST_CONNECTION_STRING")
            ?? $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=true";
    }

    /// <summary>Fake external auto-fill clients shared by all tests (counts calls).</summary>
    public FakeAutofillClients FakeAutofill { get; } = new();

    static ContentApiFactory()
    {
        Environment.SetEnvironmentVariable("Database__Provider", IsPostgres ? "Postgres" : "SqlServer");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Messaging__Transport", "InMemory");
        Environment.SetEnvironmentVariable("FileStorage__BasePath", Path.Combine(Path.GetTempPath(), "wordbuddy-content-tests"));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // WB-25: never call the real dictionary, Claude or audio hosts from tests.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IDictionaryClient>();
            services.RemoveAll<ISenseGenerator>();
            services.RemoveAll<IAudioDownloader>();
            services.AddSingleton(FakeAutofill);
            services.AddSingleton<IDictionaryClient>(FakeAutofill);
            services.AddSingleton<ISenseGenerator>(FakeAutofill);
            services.AddSingleton<IAudioDownloader>(FakeAutofill);
        });
    }

    public async Task InitializeAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }
}
