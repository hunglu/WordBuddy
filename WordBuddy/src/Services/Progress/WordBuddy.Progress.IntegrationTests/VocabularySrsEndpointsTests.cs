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
/// <c>LearnerWordConsumerTests</c>. Each test uses fresh user ids.
/// </summary>
[Collection(ProgressApiCollection.Name)]
public sealed class VocabularySrsEndpointsTests
{
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

    /// <summary>Seeds an active membership and a new state. When <paramref name="reviewedDaysAgo"/> is
    /// set, the word gets one Good review that long ago, so it is due now.</summary>
    private async Task<Guid> SeedWordAsync(Guid userId, DateTime addedAtUtc, int? reviewedDaysAgo = null)
    {
        Guid senseId = Guid.NewGuid();
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

    private async Task<List<ReviewLog>> GetLogsAsync(Guid userId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext dbContext = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        return await dbContext.ReviewLogs.AsNoTracking().Where(l => l.UserId == userId).OrderBy(l => l.AttemptNo).ToListAsync();
    }

    private static object Review(Guid sessionId, Guid senseId, bool isCorrect = true, int responseMs = 5000) => new
    {
        sessionId,
        senseId,
        exerciseType = "PictureChoice",
        skill = "Meaning",
        isCorrect,
        responseMs,
        hintUsed = false,
    };

    private static async Task<VocabularySessionDto> ReadSessionAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        VocabularySessionDto? session = await response.Content.ReadFromJsonAsync<VocabularySessionDto>(Json);
        return session!;
    }

    [Fact]
    public async Task PostReviews_EachCallAddsOneRowAndEarlierRowsNeverChange()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1));
        HttpClient client = await CreateClientAsync(userId);
        Guid sessionId = Guid.NewGuid();

        HttpResponseMessage first = await client.PostAsJsonAsync("/api/progress/vocabulary/reviews", Review(sessionId, senseId));
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        VocabularyReviewResultDto? result = await first.Content.ReadFromJsonAsync<VocabularyReviewResultDto>(Json);
        result!.Rating.Should().Be(FsrsRating.Good);
        result.Status.Should().Be(WordStatus.Learning);

        List<ReviewLog> afterFirst = await GetLogsAsync(userId);
        afterFirst.Should().ContainSingle();

        HttpResponseMessage second = await client.PostAsJsonAsync("/api/progress/vocabulary/reviews", Review(sessionId, senseId, isCorrect: false));
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        List<ReviewLog> afterSecond = await GetLogsAsync(userId);
        afterSecond.Should().HaveCount(2);
        afterSecond[0].Should().BeEquivalentTo(afterFirst[0], o => o.ComparingByMembers<ReviewLog>());
        afterSecond[0].IsDue.Should().BeTrue();
        afterSecond[1].AttemptNo.Should().Be(2);
        afterSecond[1].Rating.Should().Be(FsrsRating.Again);
    }

    [Fact]
    public async Task PostReviews_ParallelDuplicates_ApplyFsrsOnceAndNeverFail500()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1));
        Guid sessionId = Guid.NewGuid();
        HttpClient client = await CreateClientAsync(userId);

        // Same answer sent in parallel (double tap / retry). The race is real, so the loser either
        // fails the unique attempt index / RowVersion (409) or runs after the winner (200, attempt 2).
        Task<HttpResponseMessage>[] calls = Enumerable.Range(0, 5)
            .Select(_ => client.PostAsJsonAsync("/api/progress/vocabulary/reviews", Review(sessionId, senseId)))
            .ToArray();
        HttpResponseMessage[] responses = await Task.WhenAll(calls);

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
        Guid senseId = await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1));
        Guid sessionId = Guid.NewGuid();

        // Deterministic loser: this scope reads the card and counts 0 attempts, then a winner
        // request commits attempt 1 before this scope saves.
        using IServiceScope scope = _factory.Services.CreateScope();
        ILearnerWordStateRepository states = scope.ServiceProvider.GetRequiredService<ILearnerWordStateRepository>();
        IReviewLogRepository reviewLogs = scope.ServiceProvider.GetRequiredService<IReviewLogRepository>();
        LearnerWordState staleState = (await states.GetTrackedAsync(userId, senseId)).Value;

        HttpResponseMessage winner = await (await CreateClientAsync(userId))
            .PostAsJsonAsync("/api/progress/vocabulary/reviews", Review(sessionId, senseId));
        winner.StatusCode.Should().Be(HttpStatusCode.OK);

        VocabularySchedulingOptions options = new();
        staleState.ApplyReview(FsrsRating.Good, DateTime.UtcNow, new FsrsScheduler(options), options);
        await reviewLogs.AddAsync(ReviewLog.Create(
            Guid.NewGuid(), userId, senseId, sessionId, DateTime.UtcNow, ExerciseType.PictureChoice,
            VocabularySkill.Meaning, true, 5000, false, true, 1, FsrsRating.Good));
        Result save = await states.SaveChangesAsync();

        save.IsFailure.Should().BeTrue();
        save.Error.Type.Should().Be(ErrorType.Conflict);
        (await GetLogsAsync(userId)).Should().ContainSingle();
    }

    [Fact]
    public async Task PostReviews_UnknownWord_Returns404()
    {
        HttpResponseMessage response = await (await CreateClientAsync(Guid.NewGuid()))
            .PostAsJsonAsync("/api/progress/vocabulary/reviews", Review(Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostReviews_BadInput_Returns400AndWritesNothing()
    {
        Guid userId = Guid.NewGuid();
        Guid senseId = await SeedWordAsync(userId, DateTime.UtcNow.AddDays(-1));
        HttpClient client = await CreateClientAsync(userId);

        HttpResponseMessage negativeTime = await client.PostAsJsonAsync(
            "/api/progress/vocabulary/reviews", Review(Guid.NewGuid(), senseId, responseMs: -1));
        HttpResponseMessage unknownExercise = await client.PostAsJsonAsync(
            "/api/progress/vocabulary/reviews",
            new { sessionId = Guid.NewGuid(), senseId, exerciseType = "Essay", skill = "Meaning", isCorrect = true, responseMs = 10, hintUsed = false });
        HttpResponseMessage emptySession = await client.PostAsJsonAsync(
            "/api/progress/vocabulary/reviews", Review(Guid.Empty, senseId));

        negativeTime.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        unknownExercise.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        emptySession.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetLogsAsync(userId)).Should().BeEmpty();
    }

    [Fact]
    public async Task VocabularyEndpoints_WithoutToken_Return401()
    {
        HttpClient client = _factory.CreateClient();

        (await client.PostAsJsonAsync("/api/progress/vocabulary/reviews", Review(Guid.NewGuid(), Guid.NewGuid())))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/progress/vocabulary/session")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            last = await client.PostAsJsonAsync("/api/progress/vocabulary/reviews", Review(Guid.NewGuid(), Guid.NewGuid()));
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        last.Headers.RetryAfter.Should().NotBeNull();
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
            await (await CreateClientAsync(userId, ageGroup)).GetAsync("/api/progress/vocabulary/session"));

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

        VocabularySessionDto first = await ReadSessionAsync(await client.GetAsync("/api/progress/vocabulary/session"));
        foreach (VocabularySessionItemDto item in first.NewItems.Take(3))
        {
            (await client.PostAsJsonAsync("/api/progress/vocabulary/reviews", Review(first.SessionId, item.SenseId)))
                .StatusCode.Should().Be(HttpStatusCode.OK);
        }

        VocabularySessionDto second = await ReadSessionAsync(await client.GetAsync("/api/progress/vocabulary/session"));

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

        VocabularySessionDto first = await ReadSessionAsync(await client.GetAsync("/api/progress/vocabulary/session"));
        VocabularySessionDto second = await ReadSessionAsync(await client.GetAsync("/api/progress/vocabulary/session"));

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

        HttpRequestMessage valid = new(HttpMethod.Get, "/api/progress/vocabulary/session");
        valid.Headers.Add("X-Client-CurrentDateTime", local);
        HttpRequestMessage invalid = new(HttpMethod.Get, "/api/progress/vocabulary/session");
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
        VocabularySessionDto session = await ReadSessionAsync(await client.GetAsync("/api/progress/vocabulary/session"));
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
