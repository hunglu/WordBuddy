using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>
/// Runs <c>AddSenseImage</c> on a database seeded at the previous migration: existing senses keep
/// their data and get <c>ImageAssetId = NULL</c>. Uses its own throwaway database (same server as
/// <c>CONTENT_TEST_CONNECTION_STRING</c>, or LocalDB).
/// </summary>
public sealed class AddSenseImageMigrationTests : IAsyncLifetime
{
    private const string BeforeMigration = "20261005095815_AddMessagingOutbox";
    private const string ImageMigration = "20261007150225_AddSenseImage";

    private static readonly Guid LexemeId = Guid.NewGuid();
    private static readonly Guid SenseId = Guid.NewGuid();

    private readonly ContentDbContext _dbContext;

    public AddSenseImageMigrationTests()
    {
        string baseConnection = Environment.GetEnvironmentVariable("CONTENT_TEST_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Trusted_Connection=true";
        SqlConnectionStringBuilder builder = new(baseConnection)
        {
            InitialCatalog = $"WordBuddyContentImageTests_{Guid.NewGuid():N}",
        };
        _dbContext = new ContentDbContext(new DbContextOptionsBuilder<ContentDbContext>().UseSqlServer(builder.ConnectionString).Options);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.GetService<IMigrator>().MigrateAsync(BeforeMigration);

        string hash = Sense.ComputeContentHash("Dog", "An animal.", "The dog barked.");
        await _dbContext.Database.ExecuteSqlRawAsync($@"
INSERT INTO Lexemes (Id, Lemma, NormalizedLemma, WordForms, CreatedAtUtc)
VALUES ('{LexemeId}', N'Dog', N'DOG', N'[]', '2026-01-05');
INSERT INTO Senses (Id, LexemeId, Word, Definition, Example, ContentHash, Source, OwnerUserId, ShareStatus, VisibleToChildren, CreatedAtUtc)
VALUES ('{SenseId}', '{LexemeId}', N'Dog', N'An animal.', N'The dog barked.', '{hash}', N'System', '{SystemOwner.UserId}', N'Private', 0, '2026-01-05');");

        await _dbContext.GetService<IMigrator>().MigrateAsync(ImageMigration);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task AddSenseImage_Up_ExistingSenseKeepsDataAndHasNullImage()
    {
        Sense? sense = await _dbContext.Senses.AsNoTracking().SingleOrDefaultAsync(s => s.Id == SenseId);

        sense.Should().NotBeNull();
        sense!.Word.Should().Be("Dog");
        sense.ImageAssetId.Should().BeNull();
    }
}
