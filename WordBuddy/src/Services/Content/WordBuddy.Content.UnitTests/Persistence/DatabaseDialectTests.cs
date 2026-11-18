using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using WordBuddy.Content.Infrastructure.Persistence.Dialects;

namespace WordBuddy.Content.UnitTests.Persistence;

public sealed class DatabaseDialectTests
{
    private static PostgresException Pg(string sqlState, string? constraint = null) =>
        new("error", "ERROR", "ERROR", sqlState, constraintName: constraint);

    // ---- SQL Server (SqlException has no public constructor, so the number/message rule is tested directly)

    [Theory]
    [InlineData(2601, true)]
    [InlineData(2627, true)]
    [InlineData(547, false)]
    [InlineData(1205, false)]
    public void SqlServerDatabaseDialect_IsUnique_MatchesDuplicateKeyNumbers(int number, bool expected)
    {
        SqlServerDatabaseDialect.IsUnique(number, "msg", null).Should().Be(expected);
    }

    [Fact]
    public void SqlServerDatabaseDialect_IsUnique_NamedIndexMustAppearInMessage()
    {
        SqlServerDatabaseDialect.IsUnique(2601, "Cannot insert duplicate key row ... unique index 'UX_A'.", "UX_A").Should().BeTrue();
        SqlServerDatabaseDialect.IsUnique(2601, "Cannot insert duplicate key row ... unique index 'UX_B'.", "UX_A").Should().BeFalse();
    }

    [Theory]
    [InlineData(547, true)]
    [InlineData(2601, false)]
    public void SqlServerDatabaseDialect_IsForeignKey_Matches547Only(int number, bool expected)
    {
        SqlServerDatabaseDialect.IsForeignKey(number, "msg", null).Should().Be(expected);
    }

    [Fact]
    public void SqlServerDatabaseDialect_OtherExceptions_AreNotViolations()
    {
        SqlServerDatabaseDialect dialect = new();

        dialect.IsUniqueViolation(new InvalidOperationException("x")).Should().BeFalse();
        dialect.IsForeignKeyViolation(new DbUpdateException("x", new InvalidOperationException())).Should().BeFalse();
    }

    // ---- PostgreSQL

    [Fact]
    public void PostgresDatabaseDialect_UniqueViolation_IsDetectedDirectlyAndThroughDbUpdateException()
    {
        PostgresDatabaseDialect dialect = new();
        PostgresException pg = Pg(PostgresErrorCodes.UniqueViolation, "UX_Lexemes_NormalizedLemma_PartOfSpeech");

        dialect.IsUniqueViolation(pg).Should().BeTrue();
        dialect.IsUniqueViolation(new DbUpdateException("x", pg)).Should().BeTrue();
        dialect.IsForeignKeyViolation(pg).Should().BeFalse();
    }

    [Fact]
    public void PostgresDatabaseDialect_UniqueViolation_NamedIndexMustMatchConstraintName()
    {
        PostgresDatabaseDialect dialect = new();
        DbUpdateException ex = new("x", Pg(PostgresErrorCodes.UniqueViolation, "UX_B"));

        dialect.IsUniqueViolation(ex, "UX_B").Should().BeTrue();
        dialect.IsUniqueViolation(ex, "UX_A").Should().BeFalse();
    }

    [Fact]
    public void PostgresDatabaseDialect_ForeignKeyViolation_MatchesSqlStateAndName()
    {
        PostgresDatabaseDialect dialect = new();
        DbUpdateException ex = new("x", Pg(PostgresErrorCodes.ForeignKeyViolation, "FK_Senses_Lexemes_LexemeId"));

        dialect.IsForeignKeyViolation(ex).Should().BeTrue();
        dialect.IsForeignKeyViolation(ex, "FK_Senses_Lexemes_LexemeId").Should().BeTrue();
        dialect.IsForeignKeyViolation(ex, "FK_Other").Should().BeFalse();
        dialect.IsUniqueViolation(ex).Should().BeFalse();
    }

    [Fact]
    public void PostgresDatabaseDialect_OtherErrors_AreNotViolations()
    {
        PostgresDatabaseDialect dialect = new();

        dialect.IsUniqueViolation(Pg(PostgresErrorCodes.DeadlockDetected)).Should().BeFalse();
        dialect.IsForeignKeyViolation(new InvalidOperationException("x")).Should().BeFalse();
    }

    [Fact]
    public void Dialects_GuardedDeleteSql_ReturnsUrlAndUsesProviderQuoting()
    {
        new SqlServerDatabaseDialect().GuardedMediaAssetDeleteSql.Should().Contain("OUTPUT deleted.[Url]");
        string pg = new PostgresDatabaseDialect().GuardedMediaAssetDeleteSql;

        pg.Should().Contain("RETURNING \"Url\"").And.NotContain("[");
    }
}
