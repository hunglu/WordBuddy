using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Content.Infrastructure.Persistence.Postgres;
using WordBuddy.Content.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-75: the <c>migrate</c> run mode on an empty database (SQL Server by default, PostgreSQL with <c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c>).</summary>
public sealed class MigrationRunnerTests
{
    private static string BuildConnectionString()
    {
        string databaseName = $"WordBuddyContentMigrate_{Guid.NewGuid():N}";
        if (ContentApiFactory.IsPostgres)
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
        if (ContentApiFactory.IsPostgres)
        {
            services.AddDbContext<ContentDbContext, PostgresContentDbContext>(options => options.UseNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<ContentDbContext, SqlServerContentDbContext>(options => options.UseSqlServer(connectionString));
        }

        await using ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            int exitCode = await MigrationRunner.RunAsync(provider);

            exitCode.Should().Be(0);
            using IServiceScope scope = provider.CreateScope();
            ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            (await dbContext.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        }
        finally
        {
            using IServiceScope scope = provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ContentDbContext>().Database.EnsureDeletedAsync();
        }
    }
}
