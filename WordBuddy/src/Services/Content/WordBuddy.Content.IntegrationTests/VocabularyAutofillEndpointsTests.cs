using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-25 vocabulary auto-fill endpoints on real SQL Server, with fake external clients.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class VocabularyAutofillEndpointsTests
{
    private readonly ContentApiFactory _factory;

    public VocabularyAutofillEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(Guid userId, string ageGroup, bool isAdmin = false)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup, isAdmin));
        return client;
    }

    /// <summary>A fresh letters-only word (the lookup validator rejects digits).</summary>
    private static string UniqueWord() =>
        "w" + new string(Guid.NewGuid().ToString("N").Select(c => (char)('a' + (Convert.ToInt32(c.ToString(), 16) % 26))).ToArray());

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private async Task<Guid> ChildWithSupporterAsync(Guid supporterId)
    {
        Guid childId = Guid.NewGuid();
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, childId, supporterId);
        return childId;
    }

    /// <summary>Runs auto-fill as an adult and returns the first sense id.</summary>
    private async Task<Guid> AutofillAsync(string word)
    {
        JsonElement result = await ReadJsonAsync(await Client(Guid.NewGuid(), "Adult").GetAsync($"/api/vocabulary/autofill?word={word}"));
        result.GetProperty("autofillUnavailable").GetBoolean().Should().BeFalse();
        return result.GetProperty("senses")[0].GetProperty("senseId").GetGuid();
    }

    private async Task AddToMineAsync(HttpClient client, Guid senseId)
    {
        HttpResponseMessage response = await client.PostAsync($"/api/vocabulary/autofill/{senseId}/add-to-mine", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private static async Task<JsonElement> MineItemAsync(HttpClient client, Guid senseId)
    {
        JsonElement mine = await ReadJsonAsync(await client.GetAsync("/api/vocabulary/mine"));
        return mine.EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == senseId);
    }

    private static async Task<bool> SensesEndpointReturnsAsync(HttpClient client, Guid senseId)
    {
        JsonElement senses = await ReadJsonAsync(await client.GetAsync($"/api/vocabulary/senses?ids={senseId}"));
        return senses.EnumerateArray().Any(s => s.GetProperty("senseId").GetGuid() == senseId);
    }

    [Fact]
    public async Task AutofillLookup_AdultNewWord_SavesEnrichedSensesAndCatalogHitMakesNoExternalCall()
    {
        string word = UniqueWord();
        HttpClient adult = Client(Guid.NewGuid(), "Adult");

        JsonElement first = await ReadJsonAsync(await adult.GetAsync($"/api/vocabulary/autofill?word={word}"));
        int dictionaryCalls = _factory.FakeAutofill.DictionaryCalls;
        int generatorCalls = _factory.FakeAutofill.GeneratorCalls;
        JsonElement second = await ReadJsonAsync(await adult.GetAsync($"/api/vocabulary/autofill?word={word}"));

        JsonElement sense = first.GetProperty("senses").EnumerateArray().Should().ContainSingle().Subject;
        sense.GetProperty("partOfSpeech").GetString().Should().Be("Noun");
        sense.GetProperty("ipaUk").GetString().Should().Be("/test-uk/");
        sense.GetProperty("audioUkUrl").GetString().Should().EndWith($"-en-GB.mp3");
        sense.GetProperty("examples").GetArrayLength().Should().Be(2);
        sense.GetProperty("translations")[0].GetProperty("locale").GetString().Should().Be("vi");
        sense.GetProperty("awaitingApproval").GetBoolean().Should().BeFalse();
        second.GetProperty("senses")[0].GetProperty("senseId").GetGuid().Should().Be(sense.GetProperty("senseId").GetGuid());
        _factory.FakeAutofill.DictionaryCalls.Should().Be(dictionaryCalls);
        _factory.FakeAutofill.GeneratorCalls.Should().Be(generatorCalls);
    }

    [Fact]
    public async Task AutofillLookup_UnknownWord_ReturnsUnavailableAndNegativeCacheSkipsDictionary()
    {
        string word = "unknown" + UniqueWord();
        HttpClient adult = Client(Guid.NewGuid(), "Adult");

        JsonElement first = await ReadJsonAsync(await adult.GetAsync($"/api/vocabulary/autofill?word={word}"));
        int calls = _factory.FakeAutofill.DictionaryCalls;
        JsonElement second = await ReadJsonAsync(await adult.GetAsync($"/api/vocabulary/autofill?word={word}"));

        first.GetProperty("autofillUnavailable").GetBoolean().Should().BeTrue();
        second.GetProperty("autofillUnavailable").GetBoolean().Should().BeTrue();
        _factory.FakeAutofill.DictionaryCalls.Should().Be(calls);
    }

    [Fact]
    public async Task AutofillLookup_GeneratorFailure_ReturnsUnavailableAndSavesNothing()
    {
        string word = "broken" + UniqueWord();

        JsonElement result = await ReadJsonAsync(await Client(Guid.NewGuid(), "Adult").GetAsync($"/api/vocabulary/autofill?word={word}"));

        result.GetProperty("autofillUnavailable").GetBoolean().Should().BeTrue();
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        string normalized = Sense.NormalizeWord(word);
        (await db.Lexemes.AnyAsync(l => l.NormalizedLemma == normalized)).Should().BeFalse();
    }

    [Fact]
    public async Task AutofillLookup_InvalidWord_Returns400()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid(), "Adult").GetAsync("/api/vocabulary/autofill?word=abc123");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AutofillLookup_OverTenCallsPerMinute_Returns429WithRetryAfter()
    {
        HttpClient adult = Client(Guid.NewGuid(), "Adult");
        string word = UniqueWord();
        HttpResponseMessage? last = null;

        for (int i = 0; i < 11; i++)
        {
            last = await adult.GetAsync($"/api/vocabulary/autofill?word={word}");
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        last.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task AutofillAddToMine_Adult_MineShowsEnrichedWordAtOnce()
    {
        Guid adultId = Guid.NewGuid();
        Guid senseId = await AutofillAsync(UniqueWord());
        HttpClient adult = Client(adultId, "Adult");

        await AddToMineAsync(adult, senseId);
        await AddToMineAsync(adult, senseId);

        JsonElement item = await MineItemAsync(adult, senseId);
        item.GetProperty("awaitingApproval").GetBoolean().Should().BeFalse();
        item.GetProperty("definition").GetString().Should().NotBeEmpty();
        item.GetProperty("origin").GetString().Should().Be("AutoFill");
        item.GetProperty("partOfSpeech").GetString().Should().Be("Noun");
        (await SensesEndpointReturnsAsync(adult, senseId)).Should().BeTrue();
    }

    [Fact]
    public async Task AutofillChild_Unapproved_NeverSeenInMineSharedSensesOrLesson()
    {
        Guid childId = await ChildWithSupporterAsync(Guid.NewGuid());
        HttpClient child = Client(childId, "Child");
        string word = UniqueWord();
        Guid senseId = await AutofillAsync(word);
        Guid lessonId = await SeedLessonWithSenseAsync(senseId);

        JsonElement lookup = await ReadJsonAsync(await child.GetAsync($"/api/vocabulary/autofill?word={word}"));
        await AddToMineAsync(child, senseId);

        JsonElement card = lookup.GetProperty("senses")[0];
        card.GetProperty("awaitingApproval").GetBoolean().Should().BeTrue();
        card.GetProperty("definition").GetString().Should().BeEmpty();

        JsonElement item = await MineItemAsync(child, senseId);
        item.GetProperty("awaitingApproval").GetBoolean().Should().BeTrue();
        item.GetProperty("definition").GetString().Should().BeEmpty();
        item.GetProperty("examples").GetArrayLength().Should().Be(0);

        JsonElement shared = await ReadJsonAsync(await child.GetAsync("/api/vocabulary/shared"));
        shared.EnumerateArray().Should().NotContain(i => i.GetProperty("id").GetGuid() == senseId);

        (await SensesEndpointReturnsAsync(child, senseId)).Should().BeFalse();

        JsonElement lesson = await ReadJsonAsync(await child.GetAsync($"/api/lessons/{lessonId}"));
        JsonElement lessonItem = lesson.GetProperty("vocabularyItems")[0];
        lessonItem.GetProperty("awaitingApproval").GetBoolean().Should().BeTrue();
        lessonItem.GetProperty("definition").GetString().Should().BeEmpty();
    }

    [Fact]
    public async Task AutofillApprove_Supporter_OnlyThatChildSeesTheWord()
    {
        Guid supporterId = Guid.NewGuid();
        Guid childId = await ChildWithSupporterAsync(supporterId);
        Guid otherChildId = await ChildWithSupporterAsync(Guid.NewGuid());
        HttpClient child = Client(childId, "Child");
        HttpClient otherChild = Client(otherChildId, "Child");
        HttpClient supporter = Client(supporterId, "Adult");
        Guid senseId = await AutofillAsync(UniqueWord());
        await AddToMineAsync(child, senseId);
        await AddToMineAsync(otherChild, senseId);

        JsonElement pending = await ReadJsonAsync(await supporter.GetAsync($"/api/vocabulary/learners/{childId}/pending-approvals"));
        HttpResponseMessage approve = await supporter.PostAsync($"/api/vocabulary/learners/{childId}/words/{senseId}/approve", null);
        HttpResponseMessage again = await supporter.PostAsync($"/api/vocabulary/learners/{childId}/words/{senseId}/approve", null);

        pending.EnumerateArray().Should().Contain(p => p.GetProperty("senseId").GetGuid() == senseId);
        approve.StatusCode.Should().Be(HttpStatusCode.NoContent);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await MineItemAsync(child, senseId)).GetProperty("awaitingApproval").GetBoolean().Should().BeFalse();
        (await SensesEndpointReturnsAsync(child, senseId)).Should().BeTrue();
        (await MineItemAsync(otherChild, senseId)).GetProperty("awaitingApproval").GetBoolean().Should().BeTrue();
        (await SensesEndpointReturnsAsync(otherChild, senseId)).Should().BeFalse();
    }

    [Fact]
    public async Task AutofillApprove_Admin_EveryChildSeesTheWord()
    {
        Guid childId = await ChildWithSupporterAsync(Guid.NewGuid());
        HttpClient child = Client(childId, "Child");
        HttpClient admin = Client(Guid.NewGuid(), "Adult", isAdmin: true);
        Guid senseId = await AutofillAsync(UniqueWord());
        await AddToMineAsync(child, senseId);

        JsonElement pending = await ReadJsonAsync(await admin.GetAsync("/api/vocabulary/moderation/autofill-pending"));
        HttpResponseMessage approve = await admin.PostAsync($"/api/vocabulary/moderation/autofill/{senseId}/approve", null);

        pending.EnumerateArray().Should().Contain(p => p.GetProperty("senseId").GetGuid() == senseId);
        approve.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await MineItemAsync(child, senseId)).GetProperty("awaitingApproval").GetBoolean().Should().BeFalse();

        Guid laterChildId = await ChildWithSupporterAsync(Guid.NewGuid());
        HttpClient laterChild = Client(laterChildId, "Child");
        JsonElement shared = await ReadJsonAsync(await laterChild.GetAsync("/api/vocabulary/shared"));
        shared.EnumerateArray().Should().Contain(i => i.GetProperty("id").GetGuid() == senseId);
        (await SensesEndpointReturnsAsync(laterChild, senseId)).Should().BeTrue();
    }

    [Fact]
    public async Task AutofillApprove_NonSupporterAndChild_Get403()
    {
        Guid childId = await ChildWithSupporterAsync(Guid.NewGuid());
        Guid senseId = await AutofillAsync(UniqueWord());
        HttpClient stranger = Client(Guid.NewGuid(), "Adult");
        HttpClient child = Client(childId, "Child");

        HttpResponseMessage pending = await stranger.GetAsync($"/api/vocabulary/learners/{childId}/pending-approvals");
        HttpResponseMessage approve = await stranger.PostAsync($"/api/vocabulary/learners/{childId}/words/{senseId}/approve", null);
        HttpResponseMessage adminQueue = await child.GetAsync("/api/vocabulary/moderation/autofill-pending");
        HttpResponseMessage adminApprove = await stranger.PostAsync($"/api/vocabulary/moderation/autofill/{senseId}/approve", null);

        pending.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        adminQueue.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        adminApprove.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AutofillSave_NullPosCatalogSense_MovedAndPrivateLearnerSenseUntouched()
    {
        string word = UniqueWord();
        Guid learnerId = Guid.NewGuid();
        Guid nullPosLexemeId = Guid.NewGuid();
        Guid systemSenseId = Guid.NewGuid();
        Guid privateSenseId = Guid.NewGuid();
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            db.Lexemes.Add(Lexeme.Create(nullPosLexemeId, word).Value);
            db.Senses.Add(Sense.CreateSystem(systemSenseId, nullPosLexemeId, word, "An old lesson meaning.", "An example."));
            db.Senses.Add(Sense.CreateLearner(privateSenseId, nullPosLexemeId, learnerId, AgeGroup.Adult, word, "My own note.", null).Value);
            await db.SaveChangesAsync();
        }

        await AutofillAsync(word);

        using IServiceScope check = _factory.Services.CreateScope();
        ContentDbContext verify = check.ServiceProvider.GetRequiredService<ContentDbContext>();
        Sense system = await verify.Senses.Include(s => s.Lexeme).SingleAsync(s => s.Id == systemSenseId);
        Sense learner = await verify.Senses.SingleAsync(s => s.Id == privateSenseId);
        system.Lexeme!.PartOfSpeech.Should().Be(PartOfSpeech.Noun);
        learner.LexemeId.Should().Be(nullPosLexemeId);
        (await verify.Lexemes.AnyAsync(l => l.Id == nullPosLexemeId)).Should().BeTrue();
    }

    [Fact]
    public async Task AutofillSave_OnlyCatalogSensesOnNullPosLexeme_OrphanLexemeDeleted()
    {
        string word = UniqueWord();
        Guid nullPosLexemeId = Guid.NewGuid();
        Guid systemSenseId = Guid.NewGuid();
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            db.Lexemes.Add(Lexeme.Create(nullPosLexemeId, word).Value);
            db.Senses.Add(Sense.CreateSystem(systemSenseId, nullPosLexemeId, word, "An old lesson meaning.", "An example."));
            await db.SaveChangesAsync();
        }

        await AutofillAsync(word);

        using IServiceScope check = _factory.Services.CreateScope();
        ContentDbContext verify = check.ServiceProvider.GetRequiredService<ContentDbContext>();
        (await verify.Lexemes.AnyAsync(l => l.Id == nullPosLexemeId)).Should().BeFalse();
        (await verify.Senses.SingleAsync(s => s.Id == systemSenseId)).LexemeId.Should().NotBe(nullPosLexemeId);
    }

    [Fact]
    public async Task AddVocabularyAutofillMigration_EnrichedFieldsRoundTripAndOldRowsAreManual()
    {
        Guid lexemeId = Guid.NewGuid();
        Guid enrichedId = Guid.NewGuid();
        Guid rawId = Guid.NewGuid();
        string word = UniqueWord();
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
            Lexeme lexeme = Lexeme.Create(lexemeId, word, PartOfSpeech.Verb).Value;
            lexeme.Enrich(PartOfSpeech.Verb, "/uk/", "/us/", "sy·lla", ["forms"], CefrLevel.B1);
            Sense sense = Sense.CreateAutoFill(enrichedId, lexemeId, word, "To test.", ["One.", "Two."], ["c"], ["s"], ["a"], ["t"], "informal", false).Value;
            sense.AddTranslation(SenseTranslation.Create(Guid.NewGuid(), enrichedId, "vi", "thu").Value);
            db.Lexemes.Add(lexeme);
            db.Senses.Add(sense);
            await db.SaveChangesAsync();

            // A row written without the WB-25 columns, as before the migration.
            string hash = Sense.ComputeContentHash(word, "Old.", null);
            await db.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO Senses (Id, LexemeId, Word, Definition, ContentHash, Source, OwnerUserId, ShareStatus, VisibleToChildren, CreatedAtUtc)
VALUES ({rawId}, {lexemeId}, {word}, N'Old.', {hash}, N'System', {SystemOwner.UserId}, N'Private', 0, {DateTime.UtcNow})");
        }

        using IServiceScope check = _factory.Services.CreateScope();
        ContentDbContext verify = check.ServiceProvider.GetRequiredService<ContentDbContext>();
        Sense loaded = await verify.Senses.Include(s => s.Translations).Include(s => s.Lexeme).SingleAsync(s => s.Id == enrichedId);
        Sense raw = await verify.Senses.SingleAsync(s => s.Id == rawId);

        loaded.Origin.Should().Be(SenseOrigin.AutoFill);
        loaded.Examples.Should().Equal("One.", "Two.");
        loaded.Example.Should().Be("One.");
        loaded.Collocations.Should().Equal("c");
        loaded.RegisterNote.Should().Be("informal");
        loaded.ChildSuitableHint.Should().BeFalse();
        loaded.Translations.Should().ContainSingle(t => t.Locale == "vi" && t.Text == "thu");
        loaded.Lexeme!.IpaUk.Should().Be("/uk/");
        loaded.Lexeme.CefrLevel.Should().Be(CefrLevel.B1);
        raw.Origin.Should().Be(SenseOrigin.Manual);
        raw.Examples.Should().BeEmpty();
        raw.TopicTags.Should().BeEmpty();
    }

    private async Task<Guid> SeedLessonWithSenseAsync(Guid senseId)
    {
        Guid lessonId = Guid.NewGuid();
        using IServiceScope scope = _factory.Services.CreateScope();
        ContentDbContext db = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        Sense sense = await db.Senses.AsTracking().SingleAsync(s => s.Id == senseId);
        Lesson lesson = new(lessonId, "Auto-fill test", "Test only.", LessonType.Vocabulary, Level.Beginner, AgeGroup.Child, isPublished: false);
        lesson.AddVocabularyWord(sense);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();
        return lessonId;
    }
}
