using FluentAssertions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WordBuddy.Content.Infrastructure.Extensions;
using WordBuddy.Content.Infrastructure.Persistence;
using WordBuddy.Content.Infrastructure.Persistence.Dialects;
using WordBuddy.Content.Infrastructure.Persistence.Postgres;
using WordBuddy.Content.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Content.UnitTests.Persistence;

public sealed class ProviderRegistrationTests
{
    private static ServiceProvider Build(string? provider, string connectionString)
    {
        Dictionary<string, string?> values = new() { ["ConnectionStrings:DefaultConnection"] = connectionString };
        if (provider is not null)
        {
            values["Database:Provider"] = provider;
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static ILockStatementProvider LockProvider(IServiceProvider sp) =>
        sp.GetRequiredService<IOptions<EntityFrameworkOutboxOptions<ContentDbContext>>>().Value.LockStatementProvider;

    [Fact]
    public void AddInfrastructure_NoProvider_ResolvesSqlServerContextDialectAndLockProvider()
    {
        using ServiceProvider sp = Build(null, "Server=x;Database=y;Trusted_Connection=true");
        using IServiceScope scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ContentDbContext>().Should().BeOfType<SqlServerContentDbContext>();
        sp.GetRequiredService<IDatabaseDialect>().Should().BeOfType<SqlServerDatabaseDialect>();
        LockProvider(sp).Should().BeOfType<SqlServerLockStatementProvider>();
    }

    [Fact]
    public void AddInfrastructure_Postgres_ResolvesPostgresContextDialectAndLockProvider()
    {
        using ServiceProvider sp = Build("Postgres", "Host=localhost;Database=y");
        using IServiceScope scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<ContentDbContext>().Should().BeOfType<PostgresContentDbContext>();
        sp.GetRequiredService<IDatabaseDialect>().Should().BeOfType<PostgresDatabaseDialect>();
        LockProvider(sp).Should().BeOfType<PostgresLockStatementProvider>();
    }

    [Fact]
    public void AddInfrastructure_UnknownProvider_Throws()
    {
        Action act = () => Build("Oracle", "x");

        act.Should().Throw<InvalidOperationException>();
    }
}
