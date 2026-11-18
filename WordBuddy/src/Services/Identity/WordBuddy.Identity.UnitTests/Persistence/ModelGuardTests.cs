using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Infrastructure.Persistence.Postgres;
using WordBuddy.Identity.Infrastructure.Persistence.SqlServer;

namespace WordBuddy.Identity.UnitTests.Persistence;

/// <summary>Guards both provider models: no drift from the migrations, SQL Server ids pinned, no SQL Server syntax on Postgres.</summary>
public sealed class ModelGuardTests
{
    /// <summary>Today's SQL Server migration ids. They must never change (they match __EFMigrationsHistory rows).</summary>
    private static readonly string[] PinnedSqlServerMigrationIds =
    [
        "20260914080724_InitialCreate",
        "20261008021803_AddSupportLinks",
        "20261009153236_AddLearnerGroups",
        "20261011125826_AddTimeZoneAndGuardian",
        "20261013034057_AddUserDateOfBirth",
        "20261015144248_AddUserIsModerator",
        "20261017083349_AddPreferredLanguage",
    ];

    private static SqlServerIdentityDbContext SqlServer() =>
        new(new DbContextOptionsBuilder<SqlServerIdentityDbContext>().UseSqlServer("Server=x;Database=y").Options);

    private static PostgresIdentityDbContext Postgres() =>
        new(new DbContextOptionsBuilder<PostgresIdentityDbContext>().UseNpgsql("Host=x;Database=y").Options);

    [Fact]
    public void SqlServerIdentityDbContext_Model_HasNoPendingChanges()
    {
        using SqlServerIdentityDbContext db = SqlServer();

        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void SqlServerIdentityDbContext_Migrations_MatchPinnedIds()
    {
        using SqlServerIdentityDbContext db = SqlServer();

        db.Database.GetMigrations().Should().Equal(PinnedSqlServerMigrationIds);
    }

    [Fact]
    public void PostgresIdentityDbContext_Model_HasNoPendingChanges()
    {
        using PostgresIdentityDbContext db = Postgres();

        db.Database.HasPendingModelChanges().Should().BeFalse();
    }

    [Fact]
    public void PostgresIdentityDbContext_Model_HasNoSqlServerSyntaxAndUsesCitextForEmail()
    {
        using PostgresIdentityDbContext db = Postgres();
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
        model.FindEntityType(typeof(User))!.FindProperty(nameof(User.Email))!.GetColumnType().Should().Be("citext");

        void Check(string? sql, string where)
        {
            if (sql is not null && (sql.Contains('[') || sql.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) || sql.Contains("N'")))
            {
                offenders.Add($"{where}: {sql}");
            }
        }
    }
}
