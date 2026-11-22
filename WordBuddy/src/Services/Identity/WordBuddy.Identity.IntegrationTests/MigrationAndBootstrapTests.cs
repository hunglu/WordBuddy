using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Infrastructure.Persistence;
using WordBuddy.Identity.Infrastructure.Persistence.Postgres;
using WordBuddy.Identity.Infrastructure.Persistence.SqlServer;
using WordBuddy.Identity.Infrastructure.Seeding;

namespace WordBuddy.Identity.IntegrationTests;

/// <summary>
/// WB-75: the <c>migrate</c> run mode and the first-admin bootstrap, each on its own empty database
/// (SQL Server by default, PostgreSQL with <c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c>).
/// </summary>
public sealed class MigrationAndBootstrapTests : IAsyncLifetime
{
    private const string AdminEmail = "first.admin@example.com";
    private const string AdminPassword = "a-long-first-password";

    private readonly string _connectionString = BuildConnectionString();
    private readonly CapturingLoggerProvider _logs = new();
    private ServiceProvider _services = null!;

    private static string BuildConnectionString()
    {
        string databaseName = $"WordBuddyIdentityMigrate_{Guid.NewGuid():N}";
        if (IdentityApiFactory.IsPostgres)
        {
            string server = Environment.GetEnvironmentVariable("WORDBUDDY_TEST_PG_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Set WORDBUDDY_TEST_PG_CONNECTION_STRING (server part only).");
            return $"{server.TrimEnd(';')};Database={databaseName}";
        }

        return $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=true";
    }

    public Task InitializeAsync()
    {
        ServiceCollection services = new();
        services.AddLogging(builder => builder.AddProvider(_logs));
        if (IdentityApiFactory.IsPostgres)
        {
            services.AddDbContext<IdentityDbContext, PostgresIdentityDbContext>(options => options.UseNpgsql(_connectionString));
        }
        else
        {
            services.AddDbContext<IdentityDbContext, SqlServerIdentityDbContext>(options => options.UseSqlServer(_connectionString));
        }

        _services = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        using (IServiceScope scope = _services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.EnsureDeletedAsync();
        }

        await _services.DisposeAsync();
    }

    private static IConfiguration Configuration(string? email, string? password) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bootstrap:Admin:Email"] = email,
            ["Bootstrap:Admin:Password"] = password,
        }).Build();

    [Fact]
    public async Task MigrationRunner_Run_AppliesAllMigrations()
    {
        int exitCode = await MigrationRunner.RunAsync(_services, Configuration(AdminEmail, AdminPassword));

        exitCode.Should().Be(0);
        using IServiceScope scope = _services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        (await dbContext.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task AdminBootstrapper_Run_CreatesAdultAdminWhenNone()
    {
        int exitCode = await MigrationRunner.RunAsync(_services, Configuration(AdminEmail, AdminPassword));

        exitCode.Should().Be(0);
        using IServiceScope scope = _services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        List<User> admins = await dbContext.Users.Where(user => user.IsAdmin).ToListAsync();
        admins.Should().ContainSingle();
        admins[0].Email.Should().Be(AdminEmail);
        admins[0].AgeGroup.Should().Be(AgeGroup.Adult);
    }

    [Fact]
    public async Task AdminBootstrapper_Run_SkipsWhenAdminExists()
    {
        (await MigrationRunner.RunAsync(_services, Configuration(AdminEmail, AdminPassword))).Should().Be(0);

        int second = await MigrationRunner.RunAsync(_services, Configuration("other.admin@example.com", AdminPassword));

        second.Should().Be(0);
        using IServiceScope scope = _services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        (await dbContext.Users.CountAsync(user => user.IsAdmin)).Should().Be(1);
    }

    [Fact]
    public async Task AdminBootstrapper_Run_FailsWhenNotConfigured()
    {
        int exitCode = await MigrationRunner.RunAsync(_services, Configuration(null, null));

        exitCode.Should().Be(1);
        using IServiceScope scope = _services.CreateScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        (await dbContext.Users.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task AdminBootstrapper_Run_DoesNotLogSecrets()
    {
        await MigrationRunner.RunAsync(_services, Configuration(AdminEmail, AdminPassword));
        await MigrationRunner.RunAsync(_services, Configuration(AdminEmail, AdminPassword));
        await MigrationRunner.RunAsync(_services, Configuration("bad", "short"));

        string all = string.Join('\n', _logs.Messages);
        all.Should().NotBeEmpty();
        all.Should().NotContain(AdminEmail).And.NotContain(AdminPassword).And.NotContain("bad").And.NotContain("short");
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<string> Messages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

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
