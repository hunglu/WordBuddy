using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Api;
using WordBuddy.Progress.Infrastructure.Persistence;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// Boots the real Progress API host against a dedicated, per-run SQL Server database (migrated on
/// startup, dropped on disposal) — never mocks EF Core, per repo convention. The connection
/// string defaults to a local SQL Server instance and can be overridden via the
/// <c>PROGRESS_TEST_CONNECTION_STRING</c> environment variable for CI.
///
/// Configuration is injected via process environment variables, not
/// <c>WebApplicationFactory.ConfigureWebHost</c>'s <c>ConfigureAppConfiguration</c> — see
/// Content's <c>ContentApiFactory</c> for the full explanation of why (Progress.Api's
/// <c>Program.Main</c> has the identical minimal-hosting shape and reads <c>builder.Configuration</c>
/// synchronously before that hook would ever run).
/// </summary>
public sealed class ProgressApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecret = "integration-test-secret-do-not-use-in-production-32chars";
    public const string JwtIssuer = "WordBuddy";

    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("PROGRESS_TEST_CONNECTION_STRING")
        ?? $"Server=(localdb)\\mssqllocaldb;Database=WordBuddyProgressTests_{Guid.NewGuid():N};Trusted_Connection=true";

    static ProgressApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
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
