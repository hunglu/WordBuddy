using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using WordBuddy.Quiz.Domain;
using WordBuddy.Quiz.Infrastructure.Persistence.Postgres;
using WordBuddy.Quiz.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Quiz.UnitTests.Persistence;

/// <summary>Guards both provider models: no drift from the migrations, SQL Server id pinned, no SQL Server syntax on Postgres.</summary>
public sealed class ModelGuardTests
{
    private static SqlServerQuizDbContext SqlServer() =>
        new(new DbContextOptionsBuilder<SqlServerQuizDbContext>().UseSqlServer("Server=x;Database=y").Options);

    private static PostgresQuizDbContext Postgres() =>
        new(new DbContextOptionsBuilder<PostgresQuizDbContext>().UseNpgsql("Host=x;Database=y").Options);

    [Fact]
    public void SqlServerQuizDbContext_Model_HasNoPendingChanges()
    {
        using SqlServerQuizDbContext db = SqlServer();

        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void SqlServerQuizDbContext_Migrations_MatchPinnedId()
    {
        using SqlServerQuizDbContext db = SqlServer();

        // The id matches the __EFMigrationsHistory row of existing databases; it must never change.
        db.Database.GetMigrations().Should().Equal("20260914140137_InitialCreate");
    }

    [Fact]
    public void PostgresQuizDbContext_Model_HasNoPendingChanges()
    {
        using PostgresQuizDbContext db = Postgres();

        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void PostgresQuizDbContext_Model_HasNoSqlServerTypesAndOptionsAreText()
    {
        using PostgresQuizDbContext db = Postgres();
        IModel model = db.GetService<IDesignTimeModel>().Model;

        List<string> offenders = [];
        foreach (IEntityType entity in model.GetEntityTypes())
        {
            foreach (IProperty property in entity.GetProperties())
            {
                string? type = property.GetColumnType();
                if (type is not null && type.Contains("nvarchar", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add($"{entity.DisplayName()}.{property.Name}: {type}");
                }
            }
        }

        offenders.Should().BeEmpty();
        model.FindEntityType(typeof(QuizQuestion))!.FindProperty(nameof(QuizQuestion.Options))!.GetColumnType().Should().Be("text");
    }
}
