using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WordBuddy.Progress.Infrastructure.Persistence.Dialects;

namespace WordBuddy.Progress.UnitTests.Persistence;

public sealed class DatabaseDialectTests
{
    private static PostgresException Pg(string sqlState) => new("error", "ERROR", "ERROR", sqlState);

    // ---- SQL Server (SqlException has no public constructor, so the number rules are tested directly)

    [Theory]
    [InlineData(2601, true)]
    [InlineData(2627, true)]
    [InlineData(1205, false)]
    [InlineData(547, false)]
    public void SqlServerDatabaseDialect_IsUnique_MatchesDuplicateKeyNumbers(int number, bool expected)
    {
        SqlServerDatabaseDialect.IsUnique(number).Should().Be(expected);
    }

    [Theory]
    [InlineData(1205, true)]
    [InlineData(2601, false)]
    public void SqlServerDatabaseDialect_IsDeadlock_Matches1205Only(int number, bool expected)
    {
        SqlServerDatabaseDialect.IsDeadlockNumber(number).Should().Be(expected);
    }

    [Fact]
    public void SqlServerDatabaseDialect_OtherExceptions_AreNeitherUniqueNorDeadlock()
    {
        SqlServerDatabaseDialect dialect = new();
        DbUpdateException ex = new("x", new InvalidOperationException());

        dialect.IsUniqueViolation(ex).Should().BeFalse();
        dialect.IsDeadlock(ex).Should().BeFalse();
    }

    // ---- PostgreSQL

    [Fact]
    public void PostgresDatabaseDialect_UniqueViolation_DetectedDirectlyAndThroughDbUpdateException()
    {
        PostgresDatabaseDialect dialect = new();
        PostgresException pg = Pg(PostgresErrorCodes.UniqueViolation);

        dialect.IsUniqueViolation(pg).Should().BeTrue();
        dialect.IsUniqueViolation(new DbUpdateException("x", pg)).Should().BeTrue();
        dialect.IsDeadlock(pg).Should().BeFalse();
    }

    [Fact]
    public void PostgresDatabaseDialect_Deadlock_DetectedDirectlyAndThroughDbUpdateException()
    {
        PostgresDatabaseDialect dialect = new();
        PostgresException pg = Pg(PostgresErrorCodes.DeadlockDetected);

        dialect.IsDeadlock(pg).Should().BeTrue();
        dialect.IsDeadlock(new DbUpdateException("x", pg)).Should().BeTrue();
        dialect.IsUniqueViolation(pg).Should().BeFalse();
    }

    [Fact]
    public void PostgresDatabaseDialect_OtherErrors_AreNeitherUniqueNorDeadlock()
    {
        PostgresDatabaseDialect dialect = new();

        dialect.IsUniqueViolation(Pg(PostgresErrorCodes.ForeignKeyViolation)).Should().BeFalse();
        dialect.IsDeadlock(new InvalidOperationException("x")).Should().BeFalse();
    }
}
