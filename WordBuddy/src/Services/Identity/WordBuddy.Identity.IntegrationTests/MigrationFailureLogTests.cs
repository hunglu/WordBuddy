using Microsoft.Extensions.Configuration;
using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Infrastructure.Persistence;
using WordBuddy.Identity.Infrastructure.Persistence.Postgres;
using WordBuddy.Identity.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Identity.IntegrationTests;

/// <summary>WB-75: a failed <c>migrate</c> run logs type, provider error code and stack trace, never the password.</summary>
public sealed class MigrationFailureLogTests
{
    private const string Password = "Sup3rSecretPassw0rd";

    [Fact]
    public async Task MigrationRunner_Run_LogsDiagnosticsWithoutPasswordWhenDatabaseUnreachable()
    {
        List<string> messages = [];
        ServiceCollection services = new();
        services.AddLogging(builder => builder.AddProvider(new CapturingLoggerProvider(messages)));
        if (IdentityApiFactory.IsPostgres)
        {
            string connectionString = $"Host=127.0.0.1;Port=1;Database=wb;Username=wb_user;Password={Password};Timeout=2";
            services.AddDbContext<IdentityDbContext, PostgresIdentityDbContext>(options => options.UseNpgsql(connectionString));
        }
        else
        {
            string connectionString = $"Server=127.0.0.1,1;Database=wb;User Id=wb_user;Password={Password};Connect Timeout=2;TrustServerCertificate=True";
            services.AddDbContext<IdentityDbContext, SqlServerIdentityDbContext>(options => options.UseSqlServer(connectionString));
        }

        await using ServiceProvider provider = services.BuildServiceProvider();

        int exitCode = await MigrationRunner.RunAsync(provider, new ConfigurationBuilder().Build());

        exitCode.Should().Be(1);
        string all = string.Join('\n', messages);
        all.Should().Contain("migration failed").And.Contain("provider error code").And.Contain("MigrateAsync");
        all.Should().NotContain(Password);
    }

    [Fact]
    public async Task MigrationFailureLog_Write_OmitsKeyValueOnDuplicateKey()
    {
        const string Sentinel = "sentinel-key-value-4711";
        Exception failure;
        string expectedCode;
        if (IdentityApiFactory.IsPostgres)
        {
            string server = Environment.GetEnvironmentVariable("WORDBUDDY_TEST_PG_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Set WORDBUDDY_TEST_PG_CONNECTION_STRING (server part only).");
            await using NpgsqlConnection connection = new(server.TrimEnd(';') + ";Include Error Detail=true");
            await connection.OpenAsync();
            failure = await DuplicateInsertAsync(connection, "CREATE TEMP TABLE wb_dup (id text PRIMARY KEY)", "INSERT INTO wb_dup (id) VALUES ('" + Sentinel + "')");
            expectedCode = "23505";
        }
        else
        {
            await using SqlConnection connection = new(new SqlConnectionStringBuilder(
                Environment.GetEnvironmentVariable("IDENTITY_TEST_CONNECTION_STRING") ?? "Server=(localdb)\\mssqllocaldb;Trusted_Connection=true")
            {
                InitialCatalog = "master",
            }.ConnectionString);
            await connection.OpenAsync();
            failure = await DuplicateInsertAsync(connection, "CREATE TABLE #wb_dup (id nvarchar(50) PRIMARY KEY)", "INSERT INTO #wb_dup (id) VALUES (N'" + Sentinel + "')");
            expectedCode = "2627";
        }

        List<string> messages = [];
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new CapturingLoggerProvider(messages)));

        MigrationFailureLog.Write(loggerFactory.CreateLogger("test"), "WordBuddy.Test", new InvalidOperationException("outer", failure));

        string all = string.Join('\n', messages);
        all.Should().Contain(expectedCode).And.Contain("provider details").And.Contain("DuplicateInsertAsync");
        all.Should().NotContain(Sentinel);
    }

    private static async Task<Exception> DuplicateInsertAsync(DbConnection connection, string createSql, string insertSql)
    {
        await using DbCommand create = connection.CreateCommand();
        create.CommandText = createSql;
        await create.ExecuteNonQueryAsync();
        await using DbCommand insert = connection.CreateCommand();
        insert.CommandText = insertSql;
        await insert.ExecuteNonQueryAsync();
        try
        {
            await insert.ExecuteNonQueryAsync();
        }
        catch (DbException exception)
        {
            return exception;
        }

        throw new InvalidOperationException("Expected a duplicate key error.");
    }

    private sealed class CapturingLoggerProvider(List<string> messages) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(messages);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(List<string> messages) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                messages.Add(formatter(state, exception));
                if (exception is not null)
                {
                    messages.Add(exception.ToString());
                }
            }
        }
    }
}
