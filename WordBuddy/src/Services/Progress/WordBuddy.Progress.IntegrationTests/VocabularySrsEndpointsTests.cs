using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// <c>VocabularySrsController</c> against real SQL Server. Words are seeded straight into the
/// database (membership + state) so each test controls due times; the event path is covered by
/// <c>LearnerWordConsumerTests</c>. Content is a stub behind the real <c>ContentSenseClient</c>
/// (<see cref="StubContentHandler"/>). Each test uses fresh user ids.
/// </summary>
[Collection(ProgressApiCollection.Name)]
public sealed class VocabularySrsEndpointsTests
{
    private const string ExercisesUrl = "/api/progress/vocabulary/exercises";
    private const string ReviewsUrl = "/api/progress/vocabulary/reviews";
    private const string SessionUrl = "/api/progress/vocabulary/session";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ProgressApiFactory _factory;

    public VocabularySrsEndpointsTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateClientAsync(Guid userId, string ageGroup = "Adult")
    {
        if (ageGroup == "Child")
        {
            // WB-24: a child needs an active supporter for learning endpoints.
            await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, userId);
        }

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup));
        return client;
    }

    /// <summary>Seeds an active membership and a new state, and registers the sense (with a picture and audio)
    /// in the Content stub. When <paramref name="reviewedDaysAgo"/> is set, the word gets one Good review that
    /// long ago, so it is due now. Returns the sense id; its word is <see cref="WordOf"/>.</summary>
    private async Task<Guid> SeedWordAsync(
        Guid userId, DateTime addedAtUtc, int? reviewedDaysAgo = null, bool hiddenForChild = false)
    {
        Guid senseId = Guid.NewGuid();
        _factory.Content.Register(senseId, WordOf(senseId), $"https://img.test/{senseId:N}.png", $"https://aud.test/{senseId:N}.mp3", hiddenForChild);

        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();

        LearnerWordState state = LearnerWordState.CreateNew(Guid.NewGuid(), userId, senseId, addedAtUtc);
        if (reviewedDaysAgo is { } days)
        {
            VocabularySchedulingOptions options = new();
            state.ApplyReview(FsrsRating.Good, DateTime.UtcNow.AddDays(-days), new FsrsScheduler(options), options);
        }

        dbContext.LearnerWordMemberships.Add(LearnerWordMembership.CreateAdded(Guid.NewGuid(), userId, senseId, userId, addedAtUtc));
        dbContext.LearnerWordStates.Add(state);
        await dbContext.SaveChangesAsync();
        return senseId;
    }

    private static string WordOf(Guid senseId) => "w" + senseId.ToString("N")[..10];

    /// <summary>Seeds <paramref name="count"/> new words (oldest first) and opens today's session.</summary>
    private async Task<(HttpClient Client, VocabularySessionDto Session, List<Guid> SenseIds)> StartSessionAsync(
        Guid userId, int count, string ageGroup = "Adult", int hiddenForChildFrom = int.MaxValue)
    {
        HttpClient client = await CreateClientAsync(userId, ageGroup);
        List<Guid> senseIds = [];
        DateTime start = DateTime.UtcNow.AddDays(-1);
        for (int i = 0; i < count; i++)
        {
            senseIds.Add(await SeedWordAsync(userId, start.AddMinutes(i), hiddenForChild: i >= hiddenForChildFrom));
        }

        VocabularySessionDto session = await ReadSessionAsync(await client.GetAsync(SessionUrl));
        return (client, session, senseIds);
    }

    private async Task<List<ReviewLog>> GetLogsAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        return await dbContext.ReviewLogs.AsNoTracking().Where(l => l.UserId == userId).OrderBy(l => l.AttemptNo).ToListAsync();
    }

    private static async Task<VocabularySessionDto> ReadSessionAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VocabularySessionDto? session = await response.Content.ReadFromJsonAsync<VocabularySessionDto>(Json);
        return session!;
    }

    private static async Task<VocabularyExerciseDto> CreateExerciseAsync(HttpClient client, Guid sessionId, Guid senseId)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(ExercisesUrl, new { sessionId, senseId });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<VocabularyExerciseDto>(Json))!;
    }

    private static Task<HttpResponseMessage> PostAnswerAsync(
        HttpClient client, Guid exerciseId, string? optionKey = null, string? text = null, int clientMs = 0) =>
        client.PostAsJsonAsync(ReviewsUrl, new
        {
            exerciseId,
            answer = new { optionKey, text },
            clientResponseMs = clientMs,
            hintUsed = false,
        });

    /// <summary>An answer that is right for the exercise (the test knows the word).</summary>
    private static Task<HttpResponseMessage> PostRightAnswerAsync(HttpClient client, VocabularyExerciseDto exercise, Guid senseId) =>
        exercise.ExerciseType == ExerciseType.Typing
            ? PostAnswerAsync(client, exercise.ExerciseId, text: WordOf(senseId))
            : PostAnswerAsync(client, exercise.ExerciseId, optionKey: exercise.Options.Single(o => o.Text == WordOf(senseId)).Key);

    private static Task<HttpResponseMessage> PostWrongAnswerAsync(HttpClient client, VocabularyExerciseDto exercise, Guid senseId) =>
        exercise.ExerciseType == ExerciseType.Typing
            ? PostAnswerAsync(client, exercise.ExerciseId, text: "definitely-not-it")
            : PostAnswerAsync(client, exercise.ExerciseId, optionKey: exercise.Options.First(o => o.Text != WordOf(senseId)).Key);

    /// <summary>Creates an exercise for the word and answers it right. Returns the review result.</summary>
    private static async Task<VocabularyReviewResultDto> AnswerRightAsync(HttpClient client, Guid sessionId, Guid senseId)
    {
        VocabularyExerciseDto exercise = await CreateExerciseAsync(client, sessionId, senseId);
        HttpResponseMessage response = await PostRightAnswerAsync(client, exercise, senseId);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<VocabularyReviewResultDto>(Json))!;
    }

    [Fact]
    public async Task PostExercises_ReturnsPromptAndOptionsWithoutAnswerOrSenseIds()
    {
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(Guid.NewGuid(), 5);

        HttpResponseMessage response = await client.PostAsJsonAsync(ExercisesUrl, new { sessionId = session.SessionId, senseId = senseIds[0] });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string raw = await response.Content.ReadAsStringAsync();
        VocabularyExerciseDto exercise = JsonSerializer.Deserialize<VocabularyExerciseDto>(raw, Json)!;
        exercise.ExerciseType.Should().Be(ExerciseType.PictureChoice);
        exercise.Skill.Should().Be(VocabularySkill.Meaning);
        exercise.Prompt.ImageUrl.Should().Contain(senseIds[0].ToString("N"));
        exercise.Options.Should().HaveCount(4);
        exercise.Options.Select(o => o.Text).Should().Contain(WordOf(senseIds[0]));
        foreach (Guid senseId in senseIds)
        {
            raw.Should().NotContain(senseId.ToString(), "options carry opaque keys, not sense ids");
        }

        raw.ToLowerInvariant().Should().NotContain("expected").And.NotContain("iscorrect");
    }

    [Fact]
    public async Task PostExercises_FewerThanFourWords_ReturnsTypingWithoutTheWord()
    {
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(Guid.NewGuid(), 3);

        HttpResponseMessage response = await client.PostAsJsonAsync(ExercisesUrl, new { sessionId = session.SessionId, senseId = senseIds[0] });

        string raw = await response.Content.ReadAsStringAsync();
        VocabularyExerciseDto exercise = JsonSerializer.Deserialize<VocabularyExerciseDto>(raw, Json)!;
        exercise.ExerciseType.Should().Be(ExerciseType.Typing);
        exercise.Options.Should().BeEmpty();
        exercise.Prompt.Definition.Should().NotBeNullOrEmpty();
        raw.Should().NotContain(WordOf(senseIds[0]));
    }

    [Fact]
    public async Task PostExercises_NoOpenOrForeignSession_Returns404()
    {
        (_, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(Guid.NewGuid(), 4);
        HttpClient other = await CreateClientAsync(Guid.NewGuid());

        HttpResponseMessage unknownSession = await other.PostAsJsonAsync(ExercisesUrl, new { sessionId = Guid.NewGuid(), senseId = senseIds[0] });
        HttpResponseMessage foreignSession = await other.PostAsJsonAsync(ExercisesUrl, new { session.SessionId, senseId = senseIds[0] });

        unknownSession.StatusCode.Should().Be(HttpStatusCode.NotFound);
        foreignSession.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostExercises_SenseNotInSession_Returns404()
    {
        (HttpClient client, VocabularySessionDto session, _) = await StartSessionAsync(Guid.NewGuid(), 4);

        HttpResponseMessage response = await client.PostAsJsonAsync(ExercisesUrl, new { session.SessionId, senseId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostExercises_EmptyIds_Returns400()
    {
        HttpClient client = await CreateClientAsync(Guid.NewGuid());

        HttpResponseMessage response = await client.PostAsJsonAsync(ExercisesUrl, new { sessionId = Guid.Empty, senseId = Guid.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostExercises_ContentDown_Returns503AndStoresNothing()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 4);
        _factory.Content.FailWith = HttpStatusCode.BadRequest;
        try
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(ExercisesUrl, new { session.SessionId, senseId = senseIds[0] });

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }
        finally
        {
            _factory.Content.FailWith = null;
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        (await dbContext.VocabularyExercises.CountAsync(e => e.UserId == userId)).Should().Be(0);
    }

    [Fact]
    public async Task PostExercises_AskContentOncePerSession()
    {
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(Guid.NewGuid(), 4);

        foreach (Guid senseId in senseIds)
        {
            await CreateExerciseAsync(client, session.SessionId, senseId);
        }

        _factory.Content.RequestsFor(senseIds[0]).Should().ContainSingle("the senses are cached for the whole session");
    }

    [Fact]
    public async Task PostExercises_ChildToken_ContentGetsChildBearerAndHiddenSensesAreNeverUsed()
    {
        // 5 words, the last one hidden for children: 4 visible, so choice exercises are still possible.
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) =
            await StartSessionAsync(userId, 5, "Child", hiddenForChildFrom: 4);
        Guid hiddenId = senseIds[4];

        foreach (Guid senseId in senseIds.Take(4))
        {
            VocabularyExerciseDto exercise = await CreateExerciseAsync(client, session.SessionId, senseId);
            exercise.ExerciseType.Should().Be(ExerciseType.PictureChoice);
            exercise.Options.Should().HaveCount(4);
            exercise.Options.Select(o => o.Text).Should().NotContain(WordOf(hiddenId));
        }

        HttpResponseMessage hidden = await client.PostAsJsonAsync(ExercisesUrl, new { session.SessionId, senseId = hiddenId });
        hidden.StatusCode.Should().Be(HttpStatusCode.NotFound);

        string token = client.DefaultRequestHeaders.Authorization!.Parameter!;
        IReadOnlyList<StubRequest> requests = _factory.Content.RequestsFor(senseIds[0]);
        requests.Should().NotBeEmpty();
        requests.Should().OnlyContain(r => r.Scheme == "Bearer" && r.Token == token && r.AgeGroup == "Child");
    }

    [Fact]
    public async Task PostReviews_ExerciseThenRightAnswer_HappyPath()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 5);
        VocabularyExerciseDto exercise = await CreateExerciseAsync(client, session.SessionId, senseIds[0]);

        HttpResponseMessage response = await PostRightAnswerAsync(client, exercise, senseIds[0]);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VocabularyReviewResultDto result = (await response.Content.ReadFromJsonAsync<VocabularyReviewResultDto>(Json))!;
        result.IsCorrect.Should().BeTrue();
        result.CorrectAnswer.Should().Be(WordOf(senseIds[0]));
        result.Rating.Should().BeOneOf(FsrsRating.Good, FsrsRating.Easy);
        result.Status.Should().BeOneOf(WordStatus.Learning, WordStatus.Review);

        ReviewLog log = (await GetLogsAsync(userId)).Should().ContainSingle().Subject;
        log.ExerciseId.Should().Be(exercise.ExerciseId);
        log.SenseId.Should().Be(senseIds[0]);
        log.SessionId.Should().Be(session.SessionId);
        log.ExerciseType.Should().Be(ExerciseType.PictureChoice);
        log.IsCorrect.Should().BeTrue();
        log.ServerResponseMs.Should().NotBeNull();
        log.ClientResponseMs.Should().Be(0);
    }

    [Fact]
    public async Task PostReviews_TypingAnswer_IgnoresCaseAndSpaces()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 3);
        VocabularyExerciseDto exercise = await CreateExerciseAsync(client, session.SessionId, senseIds[0]);

        HttpResponseMessage response = await PostAnswerAsync(client, exercise.ExerciseId, text: "  " + WordOf(senseIds[0]).ToUpperInvariant() + " ");

        VocabularyReviewResultDto result = (await response.Content.ReadFromJsonAsync<VocabularyReviewResultDto>(Json))!;
        result.IsCorrect.Should().BeTrue();
        result.CorrectAnswer.Should().Be(WordOf(senseIds[0]));
    }

    [Fact]
    public async Task PostReviews_ForgedIsCorrectWithWrongAnswer_RatesAgainAndDoesNotImproveTheWord()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 5);
        VocabularyExerciseDto exercise = await CreateExerciseAsync(client, session.SessionId, senseIds[0]);
        string wrongKey = exercise.Options.First(o => o.Text != WordOf(senseIds[0])).Key;

        // A tampered body: the old fields are sent, with a forged "correct" flag. The server ignores them.
        HttpResponseMessage response = await client.PostAsJsonAsync(ReviewsUrl, new
        {
            exerciseId = exercise.ExerciseId,
            answer = new { optionKey = wrongKey },
            clientResponseMs = 0,
            hintUsed = false,
            isCorrect = true,
            exerciseType = "Typing",
            skill = "Usage",
            sessionId = Guid.NewGuid(),
            senseId = Guid.NewGuid(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VocabularyReviewResultDto result = (await response.Content.ReadFromJsonAsync<VocabularyReviewResultDto>(Json))!;
        result.IsCorrect.Should().BeFalse();
        result.Rating.Should().Be(FsrsRating.Again);
        result.Status.Should().Be(WordStatus.Learning);
        result.DueAtUtc.Should().BeBefore(DateTime.UtcNow.AddMinutes(5), "a wrong answer keeps the word in the short learning step");

        ReviewLog log = (await GetLogsAsync(userId)).Should().ContainSingle().Subject;
        log.IsCorrect.Should().BeFalse();
        log.Rating.Should().Be(FsrsRating.Again);
        log.SenseId.Should().Be(senseIds[0], "taken from the stored exercise, not from the body");
        log.SessionId.Should().Be(session.SessionId);
        log.ExerciseType.Should().Be(ExerciseType.PictureChoice);
        log.Skill.Should().Be(VocabularySkill.Meaning);
    }

    [Fact]
    public async Task PostReviews_ImplausibleClientTime_IsAdjustedAndLogged()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 5);
        VocabularyExerciseDto exercise = await CreateExerciseAsync(client, session.SessionId, senseIds[0]);

        // 500000 ms is far above the time the exercise has existed on the server.
        HttpResponseMessage response = await PostAnswerAsync(
            client, exercise.ExerciseId, optionKey: exercise.Options.Single(o => o.Text == WordOf(senseIds[0])).Key, clientMs: 500_000);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ReviewLog log = (await GetLogsAsync(userId)).Should().ContainSingle().Subject;
        log.TimingAdjusted.Should().BeTrue();
        log.ClientResponseMs.Should().Be(500_000);
        log.ServerResponseMs.Should().BeLessThan(60_000);
        log.ResponseMs.Should().BeLessThan(60_000);
    }

    [Fact]
    public async Task PostReviews_ReplayOfSameExercise_Returns409AndAddsNoRow()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 5);
        VocabularyExerciseDto exercise = await CreateExerciseAsync(client, session.SessionId, senseIds[0]);
        (await PostRightAnswerAsync(client, exercise, senseIds[0])).StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage replay = await PostRightAnswerAsync(client, exercise, senseIds[0]);

        replay.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await GetLogsAsync(userId)).Should().ContainSingle();
    }

    [Fact]
    public async Task PostReviews_SameExerciseInParallel_OneWinsTheRestConflict()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 5);
        VocabularyExerciseDto exercise = await CreateExerciseAsync(client, session.SessionId, senseIds[0]);

        HttpResponseMessage[] responses = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => PostRightAnswerAsync(client, exercise, senseIds[0])));

        responses.Select(r => r.StatusCode).Should().OnlyContain(c => c == HttpStatusCode.OK || c == HttpStatusCode.Conflict);
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
        (await GetLogsAsync(userId)).Should().ContainSingle();
    }

    [Fact]
    public async Task PostReviews_AnotherUsersExercise_Returns404()
    {
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(Guid.NewGuid(), 5);
        VocabularyExerciseDto exercise = await CreateExerciseAsync(client, session.SessionId, senseIds[0]);
        HttpClient thief = await CreateClientAsync(Guid.NewGuid());

        HttpResponseMessage response = await PostRightAnswerAsync(thief, exercise, senseIds[0]);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostReviews_EachCallAddsOneRowAndEarlierRowsNeverChange()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 5);

        VocabularyReviewResultDto first = await AnswerRightAsync(client, session.SessionId, senseIds[0]);
        first.Rating.Should().BeOneOf(FsrsRating.Good, FsrsRating.Easy);
        first.Status.Should().BeOneOf(WordStatus.Learning, WordStatus.Review);

        List<ReviewLog> afterFirst = await GetLogsAsync(userId);
        afterFirst.Should().ContainSingle();

        VocabularyExerciseDto second = await CreateExerciseAsync(client, session.SessionId, senseIds[0]);
        second.ExerciseType.Should().BeOneOf(new[] { ExerciseType.ListeningChoice, ExerciseType.Typing }, "after a correct answer the word moves to the recall level");
        (await PostWrongAnswerAsync(client, second, senseIds[0])).StatusCode.Should().Be(HttpStatusCode.OK);

        List<ReviewLog> afterSecond = await GetLogsAsync(userId);
        afterSecond.Should().HaveCount(2);
        afterSecond[0].Should().BeEquivalentTo(afterFirst[0], o => o.ComparingByMembers<ReviewLog>());
        afterSecond[0].IsDue.Should().BeTrue();
        afterSecond[1].AttemptNo.Should().Be(2);
        afterSecond[1].Rating.Should().Be(FsrsRating.Again);
    }

    [Fact]
    public async Task PostReviews_ParallelAnswersForSameWord_ApplyFsrsOnceAndNeverFail500()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 5);
        Guid senseId = senseIds[0];

        // Five exercises for the same word, answered in parallel (double tap / retry). The race is real,
        // so a loser either fails the unique attempt index / RowVersion (409) or runs after the winner (200, attempt 2).
        List<VocabularyExerciseDto> exercises = [];
        for (int i = 0; i < 5; i++)
        {
            exercises.Add(await CreateExerciseAsync(client, session.SessionId, senseId));
        }

        HttpResponseMessage[] responses = await Task.WhenAll(exercises.Select(e => PostRightAnswerAsync(client, e, senseId)));

        responses.Select(r => r.StatusCode).Should().OnlyContain(c => c == HttpStatusCode.OK || c == HttpStatusCode.Conflict);
        int okCount = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        okCount.Should().BeGreaterThan(0);

        List<ReviewLog> logs = await GetLogsAsync(userId);
        logs.Should().HaveCount(okCount);
        logs.Select(l => l.AttemptNo).Should().OnlyHaveUniqueItems();
        logs.Count(l => l.AttemptNo == 1).Should().Be(1);

        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        LearnerWordState state = await dbContext.LearnerWordStates.AsNoTracking()
            .SingleAsync(s => s.UserId == userId && s.SenseId == senseId);
        state.Reps.Should().Be(1);
    }

    [Fact]
    public async Task LearnerWordStateRepository_SaveChangesAsync_StaleDuplicateReturnsConflict()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 5);
        Guid senseId = senseIds[0];

        // Deterministic loser: this scope reads the card and counts 0 attempts, then a winner
        // request commits attempt 1 before this scope saves.
        using IServiceScope scope = _factory.Services.CreateScope();
        ILearnerWordStateRepository states = scope.ServiceProvider.GetRequiredService<ILearnerWordStateRepository>();
        IReviewLogRepository reviewLogs = scope.ServiceProvider.GetRequiredService<IReviewLogRepository>();
        LearnerWordState staleState = (await states.GetTrackedAsync(userId, senseId)).Value;

        (await AnswerRightAsync(client, session.SessionId, senseId)).IsCorrect.Should().BeTrue();

        VocabularySchedulingOptions options = new();
        staleState.ApplyReview(FsrsRating.Good, DateTime.UtcNow, new FsrsScheduler(options), options);
        await reviewLogs.AddAsync(ReviewLog.Create(
            Guid.NewGuid(), userId, senseId, session.SessionId, DateTime.UtcNow, ExerciseType.PictureChoice,
            VocabularySkill.Meaning, true, 5000, false, true, 1, FsrsRating.Good));
        Result save = await states.SaveChangesAsync();

        save.IsFailure.Should().BeTrue();
        save.Error.Type.Should().Be(ErrorType.Conflict);
        (await GetLogsAsync(userId)).Should().ContainSingle();
    }

    [Fact]
    public async Task PostReviews_UnknownExercise_Returns404()
    {
        HttpResponseMessage response = await PostAnswerAsync(await CreateClientAsync(Guid.NewGuid()), Guid.NewGuid(), optionKey: "abc");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostReviews_BadInput_Returns400AndWritesNothing()
    {
        Guid userId = Guid.NewGuid();
        (HttpClient client, VocabularySessionDto session, List<Guid> senseIds) = await StartSessionAsync(userId, 5);
        VocabularyExerciseDto exercise = await CreateExerciseAsync(client, session.SessionId, senseIds[0]);
        string key = exercise.Options[0].Key;

        HttpResponseMessage negativeTime = await PostAnswerAsync(client, exercise.ExerciseId, optionKey: key, clientMs: -1);
        HttpResponseMessage emptyExercise = await PostAnswerAsync(client, Guid.Empty, optionKey: key);
        HttpResponseMessage emptyAnswer = await PostAnswerAsync(client, exercise.ExerciseId);
        HttpResponseMessage noAnswer = await client.PostAsJsonAsync(ReviewsUrl, new { exerciseId = exercise.ExerciseId, clientResponseMs = 10, hintUsed = false });

        negativeTime.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        emptyExercise.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        emptyAnswer.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        noAnswer.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetLogsAsync(userId)).Should().BeEmpty();

        // The exercise is still open after the bad calls.
        (await PostAnswerAsync(client, exercise.ExerciseId, optionKey: key)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VocabularyEndpoints_WithoutToken_Return401()
    {
        HttpClient client = _factory.CreateClient();

        (await PostAnswerAsync(client, Guid.NewGuid(), optionKey: "abc")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync(ExercisesUrl, new { sessionId = Guid.NewGuid(), senseId = Guid.NewGuid() }))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync(SessionUrl)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/progress/vocabulary/words")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/progress/vocabulary/settings")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostReviews_OverRateLimit_Returns429WithRetryAfter()
    {
        HttpClient client = await CreateClientAsync(Guid.NewGuid());
        HttpResponseMessage? last = null;

        for (int i = 0; i < 61; i++)
        {
            last = await PostAnswerAsync(client, Guid.NewGuid(), optionKey: "abc");
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        last.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task Migration_OldReviewLogRows_StayReadable()
    {
        Guid userId = Guid.NewGuid();
        Guid rowId = Guid.NewGuid();
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();

            // A row shaped like one written before WB-28: none of the new columns is set.
            await dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ReviewLogs (Id, UserId, SenseId, SessionId, OccurredAtUtc, ExerciseType, Skill, IsCorrect,
                                        ResponseMs, HintUsed, IsDue, AttemptNo, Rating)
                VALUES ({rowId}, {userId}, {Guid.NewGuid()}, {Guid.NewGuid()}, {DateTime.UtcNow}, 'Typing', 'Spelling', 1,
                        4200, 0, 1, 1, 'Good')");
        }

        List<ReviewLog> logs = await GetLogsAsync(userId);

        ReviewLog old = logs.Should().ContainSingle().Subject;
        old.Id.Should().Be(rowId);
        old.ResponseMs.Should().Be(4200);
        old.ExerciseId.Should().BeNull();
        old.ClientResponseMs.Should().BeNull();
        old.ServerResponseMs.Should().BeNull();
        old.TimingAdjusted.Should().BeFalse();
    }

    [Theory]
    [InlineData("Child")]
    [InlineData("Adult")]
    public async Task GetSession_DueBeforeNew_SameCapForChildAndAdult(string ageGroup)
    {
        Guid userId = Guid.NewGuid();
        DateTime start = DateTime.UtcNow.AddDays(-30);
        Guid dueSenseId = await SeedWordAsync(userId, start, reviewedDaysAgo: 10);
        List<Guid> newSenseIds = [];
        for (int i = 0; i < 12; i++)
        {
            newSenseIds.Add(await SeedWordAsync(userId, start.AddMinutes(i)));
        }

        VocabularySessionDto session = await ReadSessionAsync(
            await (await CreateClientAsync(userId, ageGroup)).GetAsync(SessionUrl));

        session.SessionId.Should().NotBeEmpty();
        session.DueItems.Should().ContainSingle().Which.SenseId.Should().Be(dueSenseId);
        session.NewWordCap.Should().Be(10);
        session.NewWordsIntroducedToday.Should().Be(0);
        session.NewItems.Select(i => i.SenseId).Should().Equal(newSenseIds.Take(10), "oldest added first, up to the cap");
    }

    [Fact]
    public async Task GetSession_AnsweredNewWordsCountAgainstTodaysCap()
    {
        Guid userId = Guid.NewGuid();
        HttpClient client = await CreateClientAsync(userId, "Child");
        DateTime start = DateTime.UtcNow.AddDays(-1);
        for (int i = 0; i < 12; i++)
        {
            await SeedWordAsync(userId, start.AddMinutes(i));
        }

        VocabularySessionDto first = await ReadSessionAsync(await client.GetAsync(SessionUrl));
        foreach (VocabularySessionItemDto item in first.NewItems.Take(3))
        {
            (await AnswerRightAsync(client, first.SessionId, item.SenseId)).IsCorrect.Should().BeTrue();
        }

        VocabularySessionDto second = await ReadSessionAsync(await client.GetAsync(SessionUrl));

        second.NewWordsIntroducedToday.Should().Be(3);
        second.NewItems.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetSession_CalledTwice_ResumesSameSessionId()
    {
        Guid userId = Guid.NewGuid();
        HttpClient client = await CreateClientAsync(userId);
        for (int i = 0; i < 4; i++)
        {
            await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1).AddMinutes(i));
        }

        VocabularySessionDto first = await ReadSessionAsync(await client.GetAsync(SessionUrl));
        VocabularySessionDto second = await ReadSessionAsync(await client.GetAsync(SessionUrl));

        second.SessionId.Should().Be(first.SessionId);
        second.NewItems.Select(i => i.SenseId).Should().Equal(first.NewItems.Select(i => i.SenseId));
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        (await dbContext.VocabularySessionIssues.CountAsync(i => i.UserId == userId)).Should().Be(1);
    }

    [Fact]
    public async Task GetSession_ClientDateTimeHeader_ValidAccepted_InvalidReturns400()
    {
        HttpClient client = await CreateClientAsync(Guid.NewGuid());
        string local = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).ToString("yyyy-MM-dd'T'HH:mm:sszzz");

        HttpRequestMessage valid = new(HttpMethod.Get, SessionUrl);
        valid.Headers.Add("X-Client-CurrentDateTime", local);
        HttpRequestMessage invalid = new(HttpMethod.Get, SessionUrl);
        invalid.Headers.Add("X-Client-CurrentDateTime", "yesterday");

        (await client.SendAsync(valid)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.SendAsync(invalid)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWords_ReturnsOnlyCallersActiveStates()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1));
        await SeedWordAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1));

        HttpResponseMessage response = await (await CreateClientAsync(userId)).GetAsync("/api/progress/vocabulary/words");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<LearnerWordStateDto>? words = await response.Content.ReadFromJsonAsync<List<LearnerWordStateDto>>(Json);
        words.Should().ContainSingle().Which.Should().BeEquivalentTo(new { SenseId = senseId, Status = WordStatus.New, Reps = 0, Lapses = 0 });
    }

    [Theory]
    [InlineData("Child")]
    [InlineData("Adult")]
    public async Task Settings_GetDefaultThenPut_Returns200ForChildAndAdult(string ageGroup)
    {
        // D-4: child accounts use the same settings as other users (not 403).
        Guid userId = Guid.NewGuid();
        HttpClient client = await CreateClientAsync(userId, ageGroup);

        VocabularySettingsDto? initial = await client.GetFromJsonAsync<VocabularySettingsDto>("/api/progress/vocabulary/settings", Json);
        initial!.NewWordsPerDay.Should().BeNull();

        HttpResponseMessage put = await client.PutAsJsonAsync("/api/progress/vocabulary/settings", new { newWordsPerDay = 3 });
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        VocabularySettingsDto? saved = await client.GetFromJsonAsync<VocabularySettingsDto>("/api/progress/vocabulary/settings", Json);
        saved!.NewWordsPerDay.Should().Be(3);

        await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1));
        await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1));
        await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1));
        await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1));
        VocabularySessionDto session = await ReadSessionAsync(await client.GetAsync(SessionUrl));
        session.NewWordCap.Should().Be(3);
        session.NewItems.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(51)]
    public async Task Settings_PutOutOfRange_Returns400(int value)
    {
        HttpResponseMessage put = await (await CreateClientAsync(Guid.NewGuid()))
            .PutAsJsonAsync("/api/progress/vocabulary/settings", new { newWordsPerDay = value });

        put.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
