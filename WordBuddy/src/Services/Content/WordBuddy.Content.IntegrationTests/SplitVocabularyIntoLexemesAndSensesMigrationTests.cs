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
/// Runs the <c>SplitVocabularyIntoLexemesAndSenses</c> migration against a real SQL Server database
/// seeded in the old schema (at <c>DropVocabularyWordIdRemaps</c>), checks that every row survives
/// unchanged, then runs <c>Down</c>. Uses its own throwaway database (same server as
/// <c>CONTENT_TEST_CONNECTION_STRING</c>, or LocalDB).
/// </summary>
public sealed class SplitVocabularyIntoLexemesAndSensesMigrationTests : IAsyncLifetime
{
    private const string BeforeMigration = "20261002174303_DropVocabularyWordIdRemaps";
    private const string SplitMigration = "20261005023005_SplitVocabularyIntoLexemesAndSenses";

    private const string WordColumns =
        "Id, Word, Definition, Example, ContentHash, AudioAssetId, Source, OwnerUserId, OwnerAgeGroup, " +
        "ShareStatus, VisibleToChildren, CreatedAtUtc, ModeratedAtUtc, ModeratedByUserId";

    private static readonly Guid Lesson1 = Guid.NewGuid();
    private static readonly Guid Lesson2 = Guid.NewGuid();
    private static readonly Guid AudioId = Guid.NewGuid();

    private static readonly Guid OwnerA = Guid.NewGuid();
    private static readonly Guid OwnerB = Guid.NewGuid();
    private static readonly Guid OwnerC = Guid.NewGuid();
    private static readonly Guid OwnerD = Guid.NewGuid();
    private static readonly Guid OwnerE = Guid.NewGuid();
    private static readonly Guid OwnerF = Guid.NewGuid();
    private static readonly Guid ChildOwner = Guid.NewGuid();
    private static readonly Guid Adopter = Guid.NewGuid();
    private static readonly Guid Moderator = Guid.NewGuid();

    private static readonly Guid SysDog = Guid.NewGuid();      // System "Dog", audio, in both lessons
    private static readonly Guid SysCat = Guid.NewGuid();      // System "Cat", in both lessons
    private static readonly Guid LearnerDog = Guid.NewGuid();  // "DOG", older than SysDog (System still wins)
    private static readonly Guid AppleA = Guid.NewGuid();      // " Apple", owner A, newer
    private static readonly Guid AppleB = Guid.NewGuid();      // "apple", owner B, older (lemma source)
    private static readonly Guid PearShared = Guid.NewGuid();  // "pear", Shared, VisibleToChildren = 1
    private static readonly Guid PearOld = Guid.NewGuid();     // "Pear", Private, older (Shared still wins)
    private static readonly Guid KiwiShared = Guid.NewGuid();  // "kiwi", Shared, VisibleToChildren = 0
    private static readonly Guid ChildPlum = Guid.NewGuid();   // "plum", Child owner, Private

    private readonly string _connectionString;
    private readonly ContentDbContext _dbContext;

    private List<object?[]> _wordsBefore = [];
    private List<object?[]> _linksBefore = [];
    private List<object?[]> _lessonLinksBefore = [];
    private Dictionary<Guid, string> _normalizedWordBefore = [];

    public SplitVocabularyIntoLexemesAndSensesMigrationTests()
    {
        string baseConnection = Environment.GetEnvironmentVariable("CONTENT_TEST_CONNECTION_STRING")
            ?? "Server=(localdb)\\mssqllocaldb;Trusted_Connection=true";
        SqlConnectionStringBuilder builder = new(baseConnection)
        {
            InitialCatalog = $"WordBuddyContentSplitTests_{Guid.NewGuid():N}",
        };
        _connectionString = builder.ConnectionString;
        _dbContext = new ContentDbContext(new DbContextOptionsBuilder<ContentDbContext>().UseSqlServer(_connectionString).Options);
    }

    public async Task InitializeAsync()
    {
        await _dbContext.GetService<IMigrator>().MigrateAsync(BeforeMigration);
        await SeedOldSchemaAsync();

        _wordsBefore = await QueryAsync($"SELECT {WordColumns} FROM VocabularyWords ORDER BY Id");
        _linksBefore = await QueryAsync("SELECT Id, UserId, VocabularyWordId, AddedAtUtc, IsAuthor FROM UserVocabularyWords ORDER BY Id");
        _lessonLinksBefore = await QueryAsync("SELECT LessonId, VocabularyWordId, SortOrder FROM LessonVocabularyWords ORDER BY LessonId, VocabularyWordId");
        _normalizedWordBefore = (await QueryAsync("SELECT Id, NormalizedWord FROM VocabularyWords"))
            .ToDictionary(r => (Guid)r[0]!, r => (string)r[1]!);

        await _dbContext.GetService<IMigrator>().MigrateAsync(SplitMigration);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    private static string WordRow(
        Guid id, string word, string definition, string? example, Guid? audio, string source, Guid owner,
        string? ageGroup, string shareStatus, bool visibleToChildren, string createdAt, bool moderated = false)
    {
        string exampleSql = example is null ? "NULL" : $"N'{example}'";
        string audioSql = audio is null ? "NULL" : $"'{audio}'";
        string ageSql = ageGroup is null ? "NULL" : $"N'{ageGroup}'";
        string moderatedAt = moderated ? "'2026-02-01'" : "NULL";
        string moderatedBy = moderated ? $"'{Moderator}'" : "NULL";
        return $"('{id}', N'{word}', N'{definition}', {exampleSql}, N'{Sense.NormalizeWord(word)}', " +
               $"'{Sense.ComputeContentHash(word, definition, example)}', {audioSql}, N'{source}', '{owner}', {ageSql}, " +
               $"N'{shareStatus}', {(visibleToChildren ? 1 : 0)}, '{createdAt}', {moderatedAt}, {moderatedBy})";
    }

    private async Task SeedOldSchemaAsync()
    {
        string[] words =
        [
            WordRow(SysDog, "Dog", "An animal.", "The dog barked.", AudioId, "System", SystemOwner.UserId, null, "Private", false, "2026-01-05"),
            WordRow(SysCat, "Cat", "A pet.", "The cat sleeps.", null, "System", SystemOwner.UserId, null, "Private", false, "2026-01-05"),
            WordRow(LearnerDog, "DOG", "a loyal pet", null, null, "Learner", OwnerF, "Adult", "Private", false, "2026-01-01"),
            WordRow(AppleA, " Apple", "a red fruit", null, null, "Learner", OwnerA, "Adult", "Private", false, "2026-01-02"),
            WordRow(AppleB, "apple", "a green fruit", null, null, "Learner", OwnerB, "Adult", "Private", false, "2026-01-01"),
            WordRow(PearShared, "pear", "a fruit", null, null, "Learner", OwnerC, "Adult", "Shared", true, "2026-01-03", moderated: true),
            WordRow(PearOld, "Pear", "a sweet fruit", null, null, "Learner", OwnerD, "Adult", "Private", false, "2026-01-01"),
            WordRow(KiwiShared, "kiwi", "a fruit", null, null, "Learner", OwnerE, "Adult", "Shared", false, "2026-01-01", moderated: true),
            WordRow(ChildPlum, "plum", "a fruit", null, null, "Learner", ChildOwner, "Child", "Private", false, "2026-01-01"),
        ];

        await _dbContext.Database.ExecuteSqlRawAsync($@"
INSERT INTO Lessons (Id, Title, Description, Type, Level, TargetAgeGroup, IsPublished) VALUES
 ('{Lesson1}', N'Animals', N'desc', N'Vocabulary', N'Beginner', N'Child', 1),
 ('{Lesson2}', N'Pets', N'desc', N'Vocabulary', N'Beginner', N'Adult', 1);
INSERT INTO MediaAssets (Id, Type, Url) VALUES ('{AudioId}', N'Audio', N'media/vocab-{SysDog}-en.mp3');
INSERT INTO VocabularyWords (Id, Word, Definition, Example, NormalizedWord, ContentHash, AudioAssetId, Source, OwnerUserId, OwnerAgeGroup, ShareStatus, VisibleToChildren, CreatedAtUtc, ModeratedAtUtc, ModeratedByUserId) VALUES
 {string.Join(",\n ", words)};
INSERT INTO LessonVocabularyWords (LessonId, VocabularyWordId, SortOrder) VALUES
 ('{Lesson1}', '{SysDog}', 0), ('{Lesson1}', '{SysCat}', 1),
 ('{Lesson2}', '{SysCat}', 0), ('{Lesson2}', '{SysDog}', 1);
INSERT INTO UserVocabularyWords (Id, UserId, VocabularyWordId, AddedAtUtc, IsAuthor) VALUES
 ('{Guid.NewGuid()}', '{OwnerA}', '{AppleA}', '2026-01-02', 1),
 ('{Guid.NewGuid()}', '{OwnerB}', '{AppleB}', '2026-01-01', 1),
 ('{Guid.NewGuid()}', '{OwnerC}', '{PearShared}', '2026-01-03', 1),
 ('{Guid.NewGuid()}', '{OwnerD}', '{PearOld}', '2026-01-01', 1),
 ('{Guid.NewGuid()}', '{OwnerE}', '{KiwiShared}', '2026-01-01', 1),
 ('{Guid.NewGuid()}', '{OwnerF}', '{LearnerDog}', '2026-01-01', 1),
 ('{Guid.NewGuid()}', '{ChildOwner}', '{ChildPlum}', '2026-01-01', 1),
 ('{Guid.NewGuid()}', '{Adopter}', '{PearShared}', '2026-01-06', 0),
 ('{Guid.NewGuid()}', '{Adopter}', '{KiwiShared}', '2026-01-06', 0),
 ('{Guid.NewGuid()}', '{ChildOwner}', '{SysDog}', '2026-01-07', 0);
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

    private async Task<Dictionary<Guid, Guid>> GetLexemeIdsAsync() =>
        (await QueryAsync("SELECT Id, LexemeId FROM Senses")).ToDictionary(r => (Guid)r[0]!, r => (Guid)r[1]!);

    private async Task<string> GetLemmaAsync(Guid lexemeId) =>
        (string)(await QueryAsync($"SELECT Lemma FROM Lexemes WHERE Id = '{lexemeId}'")).Single()[0]!;

    [Fact]
    public async Task SplitVocabularyIntoLexemesAndSenses_Up_KeepsEverySenseIdAndContentHashByteIdentical()
    {
        List<object?[]> senses = await QueryAsync($"SELECT {WordColumns} FROM Senses ORDER BY Id");

        senses.Should().HaveCount(_wordsBefore.Count);
        senses.Should().BeEquivalentTo(_wordsBefore, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task SplitVocabularyIntoLexemesAndSenses_Up_CreatesOneLexemePerNormalizedWordWithNullPartOfSpeech()
    {
        List<object?[]> lexemes = await QueryAsync("SELECT Id, PartOfSpeech, NormalizedLemma FROM Lexemes");
        Dictionary<Guid, Guid> lexemeIds = await GetLexemeIdsAsync();

        lexemes.Should().HaveCount(_normalizedWordBefore.Values.Distinct().Count());
        lexemes.Should().OnlyContain(r => r[1] == null);

        lexemeIds[AppleA].Should().Be(lexemeIds[AppleB], "\" Apple\" and \"apple\" share one lexeme");
        lexemeIds[PearShared].Should().Be(lexemeIds[PearOld]);
        lexemeIds[SysDog].Should().Be(lexemeIds[LearnerDog]);
        lexemeIds[SysDog].Should().NotBe(lexemeIds[SysCat]);

        (await GetLemmaAsync(lexemeIds[SysDog])).Should().Be("Dog", "a System row wins over an older learner row");
        (await GetLemmaAsync(lexemeIds[PearShared])).Should().Be("pear", "a Shared row wins over an older private row");
        (await GetLemmaAsync(lexemeIds[AppleA])).Should().Be("apple", "otherwise the oldest row wins");
    }

    [Fact]
    public async Task SplitVocabularyIntoLexemesAndSenses_Up_CreatesUniqueLemmaPartOfSpeechIndexWithoutFilter()
    {
        List<object?[]> index = await QueryAsync(@"
SELECT i.is_unique, i.has_filter
FROM sys.indexes i
WHERE i.name = 'UX_Lexemes_NormalizedLemma_PartOfSpeech' AND i.object_id = OBJECT_ID('Lexemes')");
        index.Should().ContainSingle();
        ((bool)index[0][0]!).Should().BeTrue();
        ((bool)index[0][1]!).Should().BeFalse();

        List<object?[]> columns = await QueryAsync(@"
SELECT c.name
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.name = 'UX_Lexemes_NormalizedLemma_PartOfSpeech'
ORDER BY ic.key_ordinal");
        columns.Select(r => (string)r[0]!).Should().Equal("NormalizedLemma", "PartOfSpeech");

        Func<Task> insertDuplicate = () => _dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO Lexemes (Id, Lemma, NormalizedLemma, PartOfSpeech, CreatedAtUtc) VALUES (NEWID(), N'apple', N'APPLE', NULL, SYSUTCDATETIME())");
        (await insertDuplicate.Should().ThrowAsync<SqlException>()).Which.Number.Should().Be(2601);
    }

    [Fact]
    public async Task SplitVocabularyIntoLexemesAndSenses_Up_KeepsLearnerWordIdsAndSetsAddedByLearner()
    {
        List<object?[]> links = await QueryAsync("SELECT Id, UserId, SenseId, AddedAtUtc, IsAuthor FROM LearnerWords ORDER BY Id");
        links.Should().BeEquivalentTo(_linksBefore, options => options.WithStrictOrdering());

        List<object?[]> extra = await QueryAsync("SELECT AddedBy, PersonalContext FROM LearnerWords");
        extra.Should().OnlyContain(r => (string)r[0]! == "Learner" && r[1] == null);
    }

    [Fact]
    public async Task SplitVocabularyIntoLexemesAndSenses_Up_RenamesLessonLinksToLessonSensesAndKeepsSortOrder()
    {
        List<object?[]> lessonLinks = await QueryAsync("SELECT LessonId, SenseId, SortOrder FROM LessonSenses ORDER BY LessonId, SenseId");
        lessonLinks.Should().BeEquivalentTo(_lessonLinksBefore, options => options.WithStrictOrdering());

        List<object?[]> names = await QueryAsync(
            "SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('LessonSenses') AND name IN ('PK_LessonSenses', 'IX_LessonSenses_SenseId')");
        names.Select(r => (string)r[0]!).Should().BeEquivalentTo(["PK_LessonSenses", "IX_LessonSenses_SenseId"]);

        List<object?[]> foreignKeys = await QueryAsync(
            "SELECT name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('LessonSenses')");
        foreignKeys.Select(r => (string)r[0]!).Should().BeEquivalentTo(["FK_LessonSenses_Lessons_LessonId", "FK_LessonSenses_Senses_SenseId"]);
    }

    [Fact]
    public async Task SplitVocabularyIntoLexemesAndSenses_Up_RenamesTablesAndCreatesSenseTranslations()
    {
        List<object?[]> oldTables = await QueryAsync(
            "SELECT name FROM sys.tables WHERE name IN ('VocabularyWords', 'UserVocabularyWords', 'LessonVocabularyWords')");
        oldTables.Should().BeEmpty();

        List<object?[]> newTables = await QueryAsync(
            "SELECT name FROM sys.tables WHERE name IN ('Senses', 'LearnerWords', 'LessonSenses', 'Lexemes', 'SenseTranslations')");
        newTables.Select(r => (string)r[0]!).Should().BeEquivalentTo(["Senses", "LearnerWords", "LessonSenses", "Lexemes", "SenseTranslations"]);

        List<object?[]> index = await QueryAsync(
            "SELECT is_unique FROM sys.indexes WHERE name = 'UX_SenseTranslations_SenseId_Locale'");
        index.Should().ContainSingle();
        ((bool)index[0][0]!).Should().BeTrue();

        List<object?[]> oldNames = await QueryAsync(
            "SELECT name FROM sys.objects WHERE name LIKE '%VocabularyWord%' UNION ALL SELECT name FROM sys.indexes WHERE name LIKE '%VocabularyWord%'");
        oldNames.Should().BeEmpty("every PK, FK and index was renamed");
    }

    [Fact]
    public async Task SplitVocabularyIntoLexemesAndSenses_Down_RestoresOldSchemaWithSameIds()
    {
        await _dbContext.GetService<IMigrator>().MigrateAsync(BeforeMigration);

        List<object?[]> words = await QueryAsync($"SELECT {WordColumns} FROM VocabularyWords ORDER BY Id");
        words.Should().BeEquivalentTo(_wordsBefore, options => options.WithStrictOrdering());

        Dictionary<Guid, string> normalized = (await QueryAsync("SELECT Id, NormalizedWord FROM VocabularyWords"))
            .ToDictionary(r => (Guid)r[0]!, r => (string)r[1]!);
        normalized.Should().BeEquivalentTo(_normalizedWordBefore);

        List<object?[]> links = await QueryAsync("SELECT Id, UserId, VocabularyWordId, AddedAtUtc, IsAuthor FROM UserVocabularyWords ORDER BY Id");
        links.Should().BeEquivalentTo(_linksBefore, options => options.WithStrictOrdering());

        List<object?[]> lessonLinks = await QueryAsync("SELECT LessonId, VocabularyWordId, SortOrder FROM LessonVocabularyWords ORDER BY LessonId, VocabularyWordId");
        lessonLinks.Should().BeEquivalentTo(_lessonLinksBefore, options => options.WithStrictOrdering());

        List<object?[]> newTables = await QueryAsync(
            "SELECT name FROM sys.tables WHERE name IN ('Senses', 'LearnerWords', 'LessonSenses', 'Lexemes', 'SenseTranslations')");
        newTables.Should().BeEmpty();
    }
}
