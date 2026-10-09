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
/// Boots the real Content API host against a dedicated, per-run SQL Server database (migrated on
/// startup, dropped on disposal) — never mocks EF Core, per repo convention. The connection
/// string defaults to a local SQL Server instance and can be overridden via the
/// <c>CONTENT_TEST_CONNECTION_STRING</c> environment variable for CI.
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

    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("CONTENT_TEST_CONNECTION_STRING")
        ?? $"Server=(localdb)\\mssqllocaldb;Database=WordBuddyContentTests_{Guid.NewGuid():N};Trusted_Connection=true";

    /// <summary>Fake external auto-fill clients shared by all tests (counts calls).</summary>
    public FakeAutofillClients FakeAutofill { get; } = new();

    static ContentApiFactory()
    {
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
