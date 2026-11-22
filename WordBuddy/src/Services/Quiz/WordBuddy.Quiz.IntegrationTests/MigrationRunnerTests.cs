using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WordBuddy.Quiz.Api.Extensions;
using WordBuddy.Quiz.Infrastructure.Persistence;
using WordBuddy.Quiz.Infrastructure.Persistence.Postgres;
using WordBuddy.Quiz.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Quiz.IntegrationTests;

/// <summary>WB-75: the <c>migrate</c> run mode on an empty database (SQL Server by default, PostgreSQL with <c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c>).</summary>
public sealed class MigrationRunnerTests
{
    private static string BuildConnectionString()
    {
        string databaseName = $"WordBuddyQuizMigrate_{Guid.NewGuid():N}";
        if (QuizApiFactory.IsPostgres)
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
        if (QuizApiFactory.IsPostgres)
        {
            services.AddDbContext<QuizDbContext, PostgresQuizDbContext>(options => options.UseNpgsql(connectionString));
        }
        else
        {
            services.AddDbContext<QuizDbContext, SqlServerQuizDbContext>(options => options.UseSqlServer(connectionString));
        }

        await using ServiceProvider provider = services.BuildServiceProvider();
        try
        {
            int exitCode = await MigrationRunner.RunAsync(provider);

            exitCode.Should().Be(0);
            using IServiceScope scope = provider.CreateScope();
            QuizDbContext dbContext = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
            (await dbContext.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        }
        finally
        {
            using IServiceScope scope = provider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<QuizDbContext>().Database.EnsureDeletedAsync();
        }
    }
}
