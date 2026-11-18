using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WordBuddy.Quiz.Infrastructure.Persistence;

namespace WordBuddy.Quiz.UnitTests.Persistence;

public sealed class DatabaseOptionsTests
{
    private static IConfiguration Config(string? provider) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(provider is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { ["Database:Provider"] = provider })
            .Build();

    [Fact]
    public void DatabaseOptions_FromConfiguration_MissingKey_ReturnsSqlServer()
    {
        DatabaseOptions.FromConfiguration(Config(null)).Provider.Should().Be(DatabaseProvider.SqlServer);
    }

    [Theory]
    [InlineData("Postgres")]
    [InlineData("postgres")]
    [InlineData("POSTGRES")]
    public void DatabaseOptions_FromConfiguration_PostgresAnyCase_ReturnsPostgres(string value)
    {
        DatabaseOptions.FromConfiguration(Config(value)).Provider.Should().Be(DatabaseProvider.Postgres);
    }

    [Theory]
    [InlineData("SqlServer")]
    [InlineData("sqlserver")]
    public void DatabaseOptions_FromConfiguration_SqlServerAnyCase_ReturnsSqlServer(string value)
    {
        DatabaseOptions.FromConfiguration(Config(value)).Provider.Should().Be(DatabaseProvider.SqlServer);
    }

    [Theory]
    [InlineData("MySql")]
    [InlineData("1")]
    public void DatabaseOptions_FromConfiguration_UnknownValue_Throws(string value)
    {
        Action act = () => DatabaseOptions.FromConfiguration(Config(value));

        act.Should().Throw<InvalidOperationException>().WithMessage("*Database:Provider*");
    }
}
