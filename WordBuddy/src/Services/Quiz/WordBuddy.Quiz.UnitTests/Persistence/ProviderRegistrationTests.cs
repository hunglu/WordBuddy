using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Quiz.Infrastructure.Extensions;
using WordBuddy.Quiz.Infrastructure.Persistence;
using WordBuddy.Quiz.Infrastructure.Persistence.Postgres;
using WordBuddy.Quiz.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Quiz.UnitTests.Persistence;

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

    [Fact]
    public void AddInfrastructure_NoProvider_ResolvesSqlServerContext()
    {
        using ServiceProvider sp = Build(null, "Server=x;Database=y;Trusted_Connection=true");
        using IServiceScope scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<QuizDbContext>().Should().BeOfType<SqlServerQuizDbContext>();
    }

    [Fact]
    public void AddInfrastructure_Postgres_ResolvesPostgresContext()
    {
        using ServiceProvider sp = Build("Postgres", "Host=localhost;Database=y");
        using IServiceScope scope = sp.CreateScope();

        scope.ServiceProvider.GetRequiredService<QuizDbContext>().Should().BeOfType<PostgresQuizDbContext>();
    }

    [Fact]
    public void AddInfrastructure_UnknownProvider_Throws()
    {
        Action act = () => Build("Oracle", "x");

        act.Should().Throw<InvalidOperationException>();
    }
}
