using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Progress.Infrastructure.Persistence.Postgres;
using WordBuddy.Progress.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>WB-75: the <c>migrate</c> run mode on an empty database (SQL Server by default, PostgreSQL with <c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c>).</summary>
public sealed class MigrationRunnerTests
{
    private static string BuildConnectionString()
    {
        string databaseName = $"WordBuddyProgressMigrate_{Guid.NewGuid():N}";
        if (ProgressApiFactory.IsPostgres)
        {
            string server = Environment.GetEnvironmentVariable("WORDBUDDY_TEST_PG_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Set WORDBUDDY_TEST_PG_CONNECTION_STRING (server part only).");
            return $"{server.TrimEnd(';')};Database={databaseName}";
        }

        return $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=true";
    }

    [Fact]
    public async Task MigrationRunner_Run_AppliesAllMigrations()
    {
        string connectionString = BuildConnectionString();
        ServiceCollection services = new();
        services.AddLogging();
        if (ProgressApiFactory.IsPostgres)
        {
            services.AddDbContext<ProgressDbContext, PostgresProgressDbContext>(options => options.UseNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<ProgressDbContext, SqlServerProgressDbContext>(options => options.UseSqlServer(connectionString));
        }

        await using ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            int exitCode = await MigrationRunner.RunAsync(provider);

            exitCode.Should().Be(0);
            using IServiceScope scope = provider.CreateScope();
            ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
            (await dbContext.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        }
        finally
        {
            using IServiceScope scope = provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ProgressDbContext>().Database.EnsureDeletedAsync();
        }
    }
}
