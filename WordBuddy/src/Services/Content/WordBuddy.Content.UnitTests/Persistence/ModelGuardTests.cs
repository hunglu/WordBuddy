using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence.Configurations;
using WordBuddy.Content.Infrastructure.Persistence.Postgres;
using WordBuddy.Content.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Content.UnitTests.Persistence;

/// <summary>Guards both provider models: no drift from the migrations, SQL Server ids pinned, no SQL Server syntax on Postgres.</summary>
public sealed class ModelGuardTests
{
    /// <summary>Today's SQL Server migration ids. They must never change (they match __EFMigrationsHistory rows).</summary>
    private static readonly string[] PinnedSqlServerMigrationIds =
    [
        "20260914102001_InitialCreate",
        "20260924082936_AddPersonalVocabularyWords",
        "20261002092015_UnifyVocabularyWords",
        "20261002174303_DropVocabularyWordIdRemaps",
        "20261005023005_SplitVocabularyIntoLexemesAndSenses",
        "20261005095815_AddMessagingOutbox",
        "20261007150225_AddSenseImage",
        "20261008023500_AddSupportLinkProjection",
        "20261009023746_AddVocabularyAutofill",
        "20261010142457_AddLearnerGroups",
    ];

    private static SqlServerContentDbContext SqlServer() =>
        new(new DbContextOptionsBuilder<SqlServerContentDbContext>().UseSqlServer("Server=x;Database=y").Options);

    private static PostgresContentDbContext Postgres() =>
        new(new DbContextOptionsBuilder<PostgresContentDbContext>().UseNpgsql("Host=x;Database=y").Options);

    [Fact]
    public void SqlServerContentDbContext_Model_HasNoPendingChanges()
    {
        using SqlServerContentDbContext db = SqlServer();

        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void SqlServerContentDbContext_Migrations_MatchPinnedIds()
    {
        using SqlServerContentDbContext db = SqlServer();

        db.Database.GetMigrations().Should().Equal(PinnedSqlServerMigrationIds);
    }

    [Fact]
    public void PostgresContentDbContext_Model_HasNoPendingChanges()
    {
        using PostgresContentDbContext db = Postgres();

        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void PostgresContentDbContext_Model_HasNoSqlServerSyntaxAndKeepsNullSemantics()
    {
        using PostgresContentDbContext db = Postgres();
        IModel model = db.GetService<IDesignTimeModel>().Model;

        List<string> offenders = [];
        foreach (IEntityType entity in model.GetEntityTypes())
        {
            foreach (IIndex index in entity.GetIndexes())
            {
                Check(index.GetFilter(), $"{entity.DisplayName()} index {index.GetDatabaseName()} filter", bracketsAllowed: false);
            }

            foreach (IProperty property in entity.GetProperties())
            {
                Check(property.GetColumnType(), $"{entity.DisplayName()}.{property.Name} type", bracketsAllowed: false);
                Check(property.GetDefaultValueSql(), $"{entity.DisplayName()}.{property.Name} default", bracketsAllowed: true);
            }
        }

        offenders.Should().BeEmpty();

        IIndex lexemeIndex = model.FindEntityType(typeof(Lexeme))!.GetIndexes()
            .Single(i => i.GetDatabaseName() == LexemeConfiguration.LemmaPartOfSpeechIndexName);
        lexemeIndex.GetAreNullsDistinct().Should().BeFalse("two (lemma, NULL) rows must collide, as on SQL Server");
        model.FindEntityType(typeof(Sense))!.FindProperty(nameof(Sense.Word))!.GetColumnType().Should().Be("citext");
        model.FindEntityType(typeof(Lexeme))!.FindProperty(nameof(Lexeme.WordForms))!.GetColumnType().Should().Be("text");

        void Check(string? sql, string where, bool bracketsAllowed)
        {
            if (sql is not null && ((!bracketsAllowed && sql.Contains('[')) || sql.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) || sql.Contains("N'")))
            {
                offenders.Add($"{where}: {sql}");
            }
        }
    }
}
