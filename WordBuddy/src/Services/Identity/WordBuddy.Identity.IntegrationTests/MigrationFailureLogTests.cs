using Microsoft.Extensions.Configuration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
