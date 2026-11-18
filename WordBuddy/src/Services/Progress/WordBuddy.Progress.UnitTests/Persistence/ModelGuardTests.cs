using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence.Postgres;
using WordBuddy.Progress.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Progress.UnitTests.Persistence;

/// <summary>Guards both provider models: no drift from the migrations, SQL Server ids pinned, no SQL Server syntax on Postgres.</summary>
public sealed class ModelGuardTests
{
    /// <summary>Today's SQL Server migration ids. They must never change (they match __EFMigrationsHistory rows).</summary>
    private static readonly string[] PinnedSqlServerMigrationIds =
    [
        "20260914141342_InitialCreate",
        "20260924084632_AddVocabularyRecall",
        "20261005100809_AddLearnerWordMembership",
        "20261007032854_AddVocabularySrs",
        "20261007050150_AddReviewConcurrencyGuards",
        "20261008023837_AddSupportLinkProjection",
        "20261009142251_AddDashboard",
        "20261010142511_AddLearnerGroupProjection",
        "20261010162311_AddServerAnswerChecking",
        "20261011130627_AddChallenges",
    ];

    private static SqlServerProgressDbContext SqlServer() =>
        new(new DbContextOptionsBuilder<SqlServerProgressDbContext>().UseSqlServer("Server=x;Database=y").Options);

    private static PostgresProgressDbContext Postgres() =>
        new(new DbContextOptionsBuilder<PostgresProgressDbContext>().UseNpgsql("Host=x;Database=y").Options);

    [Fact]
    public void SqlServerProgressDbContext_Model_HasNoPendingChanges()
    {
        using SqlServerProgressDbContext db = SqlServer();

        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void SqlServerProgressDbContext_Migrations_MatchPinnedIds()
    {
        using SqlServerProgressDbContext db = SqlServer();

        db.Database.GetMigrations().Should().Equal(PinnedSqlServerMigrationIds);
    }

    [Fact]
    public void PostgresProgressDbContext_Model_HasNoPendingChanges()
    {
        using PostgresProgressDbContext db = Postgres();

        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void PostgresProgressDbContext_Model_HasNoSqlServerSyntaxAndUsesXminAsConcurrencyToken()
    {
        using PostgresProgressDbContext db = Postgres();
        IModel model = db.GetService<IDesignTimeModel>().Model;

        List<string> offenders = [];
        foreach (IEntityType entity in model.GetEntityTypes())
        {
            foreach (IIndex index in entity.GetIndexes())
            {
                Check(index.GetFilter(), $"{entity.DisplayName()} index {index.GetDatabaseName()} filter");
            }

            foreach (IProperty property in entity.GetProperties())
            {
                Check(property.GetColumnType(), $"{entity.DisplayName()}.{property.Name} type");
                Check(property.GetDefaultValueSql(), $"{entity.DisplayName()}.{property.Name} default");
            }
        }

        offenders.Should().BeEmpty();

        IEntityType wordState = model.FindEntityType(typeof(LearnerWordState))!;
        wordState.FindProperty("RowVersion").Should().BeNull();
        IProperty xmin = wordState.FindProperty("xmin")!;
        xmin.IsConcurrencyToken.Should().BeTrue();
        xmin.GetColumnName().Should().Be("xmin");

        void Check(string? sql, string where)
        {
            if (sql is not null && (sql.Contains('[') || sql.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) || sql.Contains("N'") || sql.Contains("rowversion", StringComparison.OrdinalIgnoreCase)))
            {
                offenders.Add($"{where}: {sql}");
            }
        }
    }

    [Fact]
    public void SqlServerProgressDbContext_Model_KeepsRowVersionShadowProperty()
    {
        using SqlServerProgressDbContext db = SqlServer();
        IModel model = db.GetService<IDesignTimeModel>().Model;

        IProperty rowVersion = model.FindEntityType(typeof(LearnerWordState))!.FindProperty("RowVersion")!;

        rowVersion.IsConcurrencyToken.Should().BeTrue();
        model.FindEntityType(typeof(LearnerWordState))!.FindProperty("xmin").Should().BeNull();
    }
}
