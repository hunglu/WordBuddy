using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>
/// Runs the <c>UnifyVocabularyWords</c> migration against a real SQL Server database seeded in the
/// old schema (at <c>AddPersonalVocabularyWords</c>), checks the data rules, then runs <c>Down</c>.
/// Uses its own throwaway database (same server as <c>CONTENT_TEST_CONNECTION_STRING</c>, or LocalDB).
/// </summary>
public sealed class UnifyVocabularyWordsMigrationTests : IAsyncLifetime
{
    private const string BeforeMigration = "20260924082936_AddPersonalVocabularyWords";

    /// <summary>Stop here: the later <c>DropVocabularyWordIdRemaps</c> migration removes the remap
    /// table this test inspects.</summary>
    private const string UnifyMigration = "20261002092015_UnifyVocabularyWords";

    private static readonly Guid LessonId = Guid.NewGuid();
    private static readonly Guid AudioId = Guid.NewGuid();
    private static readonly Guid System1 = Guid.NewGuid();
    private static readonly Guid System2 = Guid.NewGuid();

    private static readonly Guid OwnerA = Guid.NewGuid();
    private static readonly Guid OwnerB = Guid.NewGuid();
    private static readonly Guid OwnerC = Guid.NewGuid();
    private static readonly Guid OwnerD = Guid.NewGuid();
    private static readonly Guid OwnerE = Guid.NewGuid();
    private static readonly Guid OwnerF = Guid.NewGuid();
    private static readonly Guid OwnerG = Guid.NewGuid();
    private static readonly Guid OwnerH = Guid.NewGuid();
    private static readonly Guid OwnerI = Guid.NewGuid();
    private static readonly Guid OwnerJ = Guid.NewGuid();
    private static readonly Guid OwnerK = Guid.NewGuid();

    private static readonly Guid A1 = Guid.NewGuid(); // apple, Private, older
    private static readonly Guid A2 = Guid.NewGuid(); // apple, Private, newer -> A1
    private static readonly Guid B1 = Guid.NewGuid(); // pear, Shared
    private static readonly Guid C1 = Guid.NewGuid(); // PEAR copy, Private -> B1 (adopted copy)
    private static readonly Guid D1 = Guid.NewGuid(); // equals system Dog, Private -> system word
    private static readonly Guid E1 = Guid.NewGuid(); // secret, Private (kept)
    private static readonly Guid F1 = Guid.NewGuid(); // secret, Private, other owner (kept)
    private static readonly Guid G1 = Guid.NewGuid(); // kiwi, Private, older -> G2
    private static readonly Guid G2 = Guid.NewGuid(); // kiwi, Shared, newer (Shared wins)
    private static readonly Guid H1 = Guid.NewGuid(); // pear, PendingReview (kept, not collapsed)
    private static readonly Guid I1 = Guid.NewGuid(); // plum, Private, Child (kept: J1 not child-visible)
    private static readonly Guid J1 = Guid.NewGuid(); // plum, Shared, VisibleToChildren = 0
    private static readonly Guid K1 = Guid.NewGuid(); // plum, Private, Adult -> J1

    private readonly string _connectionString;
    private readonly ContentDbContext _dbContext;

    public UnifyVocabularyWordsMigrationTests()
    {
        string baseConnection = Environment.GetEnvironmentVariable("CONTENT_TEST_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Trusted_Connection=true";
        SqlConnectionStringBuilder builder = new(baseConnection)
        {
            InitialCatalog = $"WordBuddyContentMigrationTests_{Guid.NewGuid():N}",
        };
        _connectionString = builder.ConnectionString;
        _dbContext = new ContentDbContext(new DbContextOptionsBuilder<ContentDbContext>().UseSqlServer(_connectionString).Options);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.GetService<IMigrator>().MigrateAsync(BeforeMigration);
        await SeedOldSchemaAsync();
        await _dbContext.GetService<IMigrator>().MigrateAsync(UnifyMigration);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    private async Task SeedOldSchemaAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync($@"
INSERT INTO Lessons (Id, Title, Description, Type, Level, TargetAgeGroup, IsPublished)
VALUES ('{LessonId}', N'Animals', N'desc', N'Vocabulary', N'Beginner', N'Child', 1);
INSERT INTO MediaAssets (Id, Type, Url) VALUES ('{AudioId}', N'Audio', N'media/vocab-{System1}-en.mp3');
INSERT INTO VocabularyItems (Id, LessonId, Word, Definition, Example, AudioAssetId) VALUES
 ('{System1}', '{LessonId}', N'Dog', N'An animal.', N'The dog barked.', '{AudioId}'),
 ('{System2}', '{LessonId}', N'Dog', N'An animal.', N'The dog barked.', NULL);
INSERT INTO PersonalVocabularyWords (Id, OwnerUserId, OwnerAgeGroup, Word, Definition, Example, ShareStatus, VisibleToChildren, CreatedAtUtc) VALUES
 ('{A1}', '{OwnerA}', N'Adult', N'apple', N'a fruit', NULL, N'Private', 0, '2026-01-01'),
 ('{A2}', '{OwnerA}', N'Adult', N' Apple', N'A fruit', NULL, N'Private', 0, '2026-01-02'),
 ('{B1}', '{OwnerB}', N'Adult', N'pear', N'a fruit', NULL, N'Shared', 1, '2026-01-01'),
 ('{C1}', '{OwnerC}', N'Child', N'PEAR', N'a fruit', NULL, N'Private', 0, '2026-01-03'),
 ('{D1}', '{OwnerD}', N'Child', N'dog', N'an animal.', N'the dog barked.', N'Private', 0, '2026-01-01'),
 ('{E1}', '{OwnerE}', N'Adult', N'secret', N'def', NULL, N'Private', 0, '2026-01-01'),
 ('{F1}', '{OwnerF}', N'Adult', N'secret', N'def', NULL, N'Private', 0, '2026-01-01'),
 ('{G1}', '{OwnerG}', N'Adult', N'kiwi', N'a fruit', NULL, N'Private', 0, '2026-01-01'),
 ('{G2}', '{OwnerG}', N'Adult', N'kiwi', N'a fruit', NULL, N'Shared', 0, '2026-01-05'),
 ('{H1}', '{OwnerH}', N'Adult', N'pear', N'a fruit', NULL, N'PendingReview', 0, '2026-01-04'),
 ('{I1}', '{OwnerI}', N'Child', N'plum', N'a fruit', NULL, N'Private', 0, '2026-01-03'),
 ('{J1}', '{OwnerJ}', N'Adult', N'plum', N'a fruit', NULL, N'Shared', 0, '2026-01-01'),
 ('{K1}', '{OwnerK}', N'Adult', N'plum', N'a fruit', NULL, N'Private', 0, '2026-01-03');
");
    }

    private async Task<List<object?[]>> QueryAsync(string sql)
    {
        DbConnection connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await using DbDataReader reader = await command.ExecuteReaderAsync();

        List<object?[]> rows = [];
        while (await reader.ReadAsync())
        {
            object?[] row = new object?[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }
        return rows;
    }

    private async Task<Dictionary<Guid, Guid>> GetRemapsAsync() =>
        (await QueryAsync("SELECT OldId, NewId FROM VocabularyWordIdRemaps")).ToDictionary(r => (Guid)r[0]!, r => (Guid)r[1]!);

    private async Task<List<(Guid UserId, Guid WordId, bool IsAuthor)>> GetLinksAsync() =>
        (await QueryAsync("SELECT UserId, VocabularyWordId, IsAuthor FROM UserVocabularyWords"))
            .Select(r => ((Guid)r[0]!, (Guid)r[1]!, (bool)r[2]!))
            .ToList();

    [Fact]
    public async Task UnifyVocabularyWords_Up_DropsOldTablesAndEveryFormerRowIsReachable()
    {
        List<object?[]> oldTables = await QueryAsync(
            "SELECT name FROM sys.tables WHERE name IN ('VocabularyItems', 'PersonalVocabularyWords')");
        oldTables.Should().BeEmpty();

        HashSet<Guid> words = (await QueryAsync("SELECT Id FROM VocabularyWords")).Select(r => (Guid)r[0]!).ToHashSet();
        Dictionary<Guid, Guid> remaps = await GetRemapsAsync();

        Guid[] formerIds = [System1, System2, A1, A2, B1, C1, D1, E1, F1, G1, G2, H1, I1, J1, K1];
        formerIds.Should().OnlyContain(id => words.Contains(id) || (remaps.ContainsKey(id) && words.Contains(remaps[id])));
    }

    [Fact]
    public async Task UnifyVocabularyWords_Up_PreservesLessonWordIdsAudioAndLessonLinks()
    {
        List<object?[]> system = await QueryAsync(
            "SELECT Id, Source, OwnerUserId, ShareStatus, AudioAssetId, ContentHash FROM VocabularyWords WHERE Source = 'System'");

        system.Select(r => (Guid)r[0]!).Should().BeEquivalentTo([System1, System2]);
        system.Should().OnlyContain(r => (Guid)r[2]! == SystemOwner.UserId && (string)r[3]! == "Private");
        system.Single(r => (Guid)r[0]! == System1)[4].Should().Be(AudioId);
        system.Should().OnlyContain(r => (string)r[5]! == VocabularyWord.ComputeContentHash("Dog", "An animal.", "The dog barked."));

        List<object?[]> lessonLinks = await QueryAsync($"SELECT VocabularyWordId FROM LessonVocabularyWords WHERE LessonId = '{LessonId}'");
        lessonLinks.Select(r => (Guid)r[0]!).Should().BeEquivalentTo([System1, System2]);
    }

    [Fact]
    public async Task UnifyVocabularyWords_Up_ComputesTheSameHashAsTheDomain()
    {
        List<object?[]> rows = await QueryAsync($"SELECT ContentHash, NormalizedWord FROM VocabularyWords WHERE Id = '{A1}'");

        rows.Should().ContainSingle();
        ((string)rows[0][0]!).Should().Be(VocabularyWord.ComputeContentHash("apple", "a fruit", null));
        ((string)rows[0][1]!).Should().Be("APPLE");
    }

    [Fact]
    public async Task UnifyVocabularyWords_Up_MergesDuplicatesAndRecordsRemaps()
    {
        Dictionary<Guid, Guid> remaps = await GetRemapsAsync();

        remaps.Should().HaveCount(5);
        remaps[A2].Should().Be(A1, "same-owner duplicates merge into the oldest");
        remaps[C1].Should().Be(B1, "an adopted copy collapses into the shared word");
        new[] { System1, System2 }.Should().Contain(remaps[D1]);
        remaps[G1].Should().Be(G2, "the Shared row wins within an owner");
        remaps[K1].Should().Be(J1, "an adult's copy collapses into a shared word even if it is not child-visible");
        remaps.Should().NotContainKeys(E1, F1, H1, I1, J1);
    }

    [Fact]
    public async Task UnifyVocabularyWords_Up_LinksEveryOwnerOnceWithCorrectAuthorship()
    {
        Dictionary<Guid, Guid> remaps = await GetRemapsAsync();
        List<(Guid UserId, Guid WordId, bool IsAuthor)> links = await GetLinksAsync();

        links.Should().BeEquivalentTo(new[]
        {
            (OwnerA, A1, true),
            (OwnerB, B1, true),
            (OwnerC, B1, false),
            (OwnerD, remaps[D1], false),
            (OwnerE, E1, true),
            (OwnerF, F1, true),
            (OwnerG, G2, true),
            (OwnerH, H1, true),
            (OwnerI, I1, true),
            (OwnerJ, J1, true),
            (OwnerK, J1, false),
        });
    }

    [Fact]
    public async Task UnifyVocabularyWords_Up_KeepsChildCopyOfNonChildVisibleSharedWord()
    {
        Dictionary<Guid, Guid> remaps = await GetRemapsAsync();
        List<(Guid UserId, Guid WordId, bool IsAuthor)> links = await GetLinksAsync();
        List<object?[]> kept = await QueryAsync($"SELECT OwnerUserId, Source, ShareStatus FROM VocabularyWords WHERE Id = '{I1}'");

        remaps.Should().NotContainKey(I1, "a Child is never linked to a shared word they could not see");
        kept.Should().ContainSingle();
        ((Guid)kept[0][0]!).Should().Be(OwnerI);
        ((string)kept[0][1]!).Should().Be("Learner");
        links.Where(l => l.UserId == OwnerI).Should().BeEquivalentTo(new[] { (OwnerI, I1, true) });
    }

    [Fact]
    public async Task UnifyVocabularyWords_Up_LeavesNoDuplicateLearnerRowsPerOwner()
    {
        List<object?[]> duplicates = await QueryAsync(
            "SELECT ContentHash, OwnerUserId FROM VocabularyWords WHERE Source = 'Learner' GROUP BY ContentHash, OwnerUserId HAVING COUNT(*) > 1");
        duplicates.Should().BeEmpty();

        List<object?[]> index = await QueryAsync(
            "SELECT is_unique, filter_definition FROM sys.indexes WHERE name = 'UX_VocabularyWords_ContentHash_OwnerUserId_Learner'");
        index.Should().ContainSingle();
        ((bool)index[0][0]!).Should().BeTrue();
    }

    [Fact]
    public async Task UnifyVocabularyWords_Down_RebuildsOldTables()
    {
        await _dbContext.GetService<IMigrator>().MigrateAsync(BeforeMigration);

        List<object?[]> items = await QueryAsync("SELECT Id, LessonId, AudioAssetId FROM VocabularyItems");
        items.Select(r => (Guid)r[0]!).Should().BeEquivalentTo([System1, System2]);
        items.Should().OnlyContain(r => (Guid)r[1]! == LessonId);

        List<object?[]> personal = await QueryAsync("SELECT Id, OwnerUserId, ShareStatus FROM PersonalVocabularyWords");
        personal.Select(r => (Guid)r[0]!).Should().Contain([A1, B1, E1, F1, G2, H1]);
        personal.Should().Contain(r => (Guid)r[1]! == OwnerC && (string)r[2]! == "Private", "a non-author link becomes a private copy");
        personal.Should().Contain(r => (Guid)r[1]! == OwnerD && (string)r[2]! == "Private");

        List<object?[]> newTables = await QueryAsync(
            "SELECT name FROM sys.tables WHERE name IN ('VocabularyWords', 'UserVocabularyWords', 'LessonVocabularyWords', 'VocabularyWordIdRemaps')");
        newTables.Should().BeEmpty();
    }
}
