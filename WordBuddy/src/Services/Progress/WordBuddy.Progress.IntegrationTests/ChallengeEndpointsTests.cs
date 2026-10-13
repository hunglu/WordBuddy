using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Application.Features.Challenges.Commands.RecomputeChallengeScores;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Progress.Infrastructure.Persistence;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// WB-29 challenge endpoints on real SQL Server: join, review logs, recompute, board for an adult and for a child
/// (consent first), <c>Challenge.Full</c>, audience rules, and the Guardian consent rules. The background clock is off;
/// tests create their own challenges and call the recompute handler by hand. Fresh ids per test.
/// </summary>
[Collection(ProgressApiCollection.Name)]
public sealed class ChallengeEndpointsTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ProgressApiFactory _factory;

    public ChallengeEndpointsTests(ProgressApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(Guid userId, string ageGroup, bool guardian = false)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup, guardian));
        return client;
    }

    private async Task<Challenge> SeedChallengeAsync(ChallengeAudience audience, int capacity = 30, ChallengeStatus status = ChallengeStatus.Running)
    {
        DateTime start = DateTime.UtcNow.Date.AddDays(-1);
        Challenge challenge = Challenge.Create(
            Guid.NewGuid(), $"it-{Guid.NewGuid():N}"[..30], audience, ChallengeKind.Ranked, ChallengeMetric.MemoryPoints, null,
            start.AddDays(-1), start, start.AddDays(6), capacity).Value;
        challenge.Open();
        if (status >= ChallengeStatus.Running)
        {
            challenge.Start();
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();
        return challenge;
    }

    private async Task SeedProfileAsync(Guid userId, AgeGroup ageGroup, string? alias = null, bool guardian = false)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        db.ChallengeProfiles.Add(ChallengeProfile.Create(userId, ageGroup, alias, "fox", ageGroup == AgeGroup.Adult ? "Real Adult Name" : "Real Child Name", null, guardian, DateTime.UtcNow));
        await db.SaveChangesAsync();
    }

    private async Task SeedConsentAsync(Guid childId, Guid guardianId)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        db.ChallengeConsents.Add(ChallengeConsent.Create(childId, guardianId, DateTime.UtcNow));
        await db.SaveChangesAsync();
    }

    /// <summary>A first (ignored) review before the window, then <paramref name="counted"/> counted answers just after the learner joined.</summary>
    private async Task SeedReviewsAsync(Guid userId, int counted)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        for (int i = 0; i < counted; i++)
        {
            Guid sense = Guid.NewGuid();
            db.ReviewLogs.Add(ReviewLog.Create(
                Guid.NewGuid(), userId, sense, Guid.NewGuid(), DateTime.UtcNow.AddDays(-2), ExerciseType.Typing, VocabularySkill.Spelling,
                isCorrect: true, responseMs: 3000, hintUsed: false, isDue: true, attemptNo: 1, FsrsRating.Good));
            db.ReviewLogs.Add(ReviewLog.Create(
                Guid.NewGuid(), userId, sense, Guid.NewGuid(), DateTime.UtcNow.AddMinutes(1), ExerciseType.Typing, VocabularySkill.Spelling,
                isCorrect: true, responseMs: 3000, hintUsed: false, isDue: true, attemptNo: 1, FsrsRating.Good));
        }

        await db.SaveChangesAsync();
    }

    private async Task RecomputeAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ICommandHandler<RecomputeChallengeScoresCommand, int> handler =
            scope.ServiceProvider.GetRequiredService<ICommandHandler<RecomputeChallengeScoresCommand, int>>();
        Result<int> result = await handler.HandleAsync(new RecomputeChallengeScoresCommand());
        result.IsSuccess.Should().BeTrue();
    }

    private static async Task<string?> ProblemTitleAsync(HttpResponseMessage response)
    {
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return problem.GetProperty("title").GetString();
    }

    [Fact]
    public async Task Challenges_AdultFlow_JoinReviewRecomputeBoard()
    {
        Guid adult = Guid.NewGuid();
        await SeedProfileAsync(adult, AgeGroup.Adult, alias: "Ada");
        Challenge challenge = await SeedChallengeAsync(ChallengeAudience.Adult);
        HttpClient client = Client(adult, "Adult");

        List<ChallengeDto> listed = (await client.GetFromJsonAsync<List<ChallengeDto>>("/api/progress/challenges", Json))!;
        listed.Should().ContainSingle(c => c.Id == challenge.Id && !c.Joined);

        (await client.PostAsync($"/api/progress/challenges/{challenge.Id}/join", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await SeedReviewsAsync(adult, counted: 2);
        await RecomputeAsync();

        ChallengeBoardDto board = (await client.GetFromJsonAsync<ChallengeBoardDto>($"/api/progress/challenges/{challenge.Id}/board", Json))!;
        board.MyRank.Should().Be(1);
        board.MyScore.Should().Be(20);
        board.Entries.Should().ContainSingle().Which.Name.Should().Be("Ada");

        ChallengeDto joined = (await client.GetFromJsonAsync<List<ChallengeDto>>("/api/progress/challenges", Json))!.Single(c => c.Id == challenge.Id);
        joined.Joined.Should().BeTrue();
        joined.MyScore.Should().Be(20);
        joined.ParticipantCount.Should().Be(1);
    }

    [Fact]
    public async Task Challenges_Leave_RemovesTheLearnerFromTheBoard()
    {
        Guid adult = Guid.NewGuid();
        Challenge challenge = await SeedChallengeAsync(ChallengeAudience.Adult);
        HttpClient client = Client(adult, "Adult");
        await client.PostAsync($"/api/progress/challenges/{challenge.Id}/join", null);

        (await client.DeleteAsync($"/api/progress/challenges/{challenge.Id}/join")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        ChallengeBoardDto board = (await client.GetFromJsonAsync<ChallengeBoardDto>($"/api/progress/challenges/{challenge.Id}/board", Json))!;
        board.ParticipantCount.Should().Be(0);
        board.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Challenges_FullChallenge_SecondLearnerGets409ChallengeFull()
    {
        Challenge challenge = await SeedChallengeAsync(ChallengeAudience.Adult, capacity: 1);

        HttpResponseMessage first = await Client(Guid.NewGuid(), "Adult").PostAsync($"/api/progress/challenges/{challenge.Id}/join", null);
        HttpResponseMessage second = await Client(Guid.NewGuid(), "Adult").PostAsync($"/api/progress/challenges/{challenge.Id}/join", null);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ProblemTitleAsync(second)).Should().Be("Challenge.Full");

        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        (await db.ChallengeParticipants.CountAsync(p => p.ChallengeId == challenge.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Challenges_ParallelJoinsAroundCapacity_AdmitExactlyCapacityAndNeverFail()
    {
        Challenge challenge = await SeedChallengeAsync(ChallengeAudience.Adult, capacity: 3);

        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            Client(Guid.NewGuid(), "Adult").PostAsync($"/api/progress/challenges/{challenge.Id}/join", null)));

        string[] outcomes = new string[responses.Length];
        for (int i = 0; i < responses.Length; i++)
        {
            string detail = responses[i].StatusCode == HttpStatusCode.NoContent ? string.Empty : await ProblemTitleAsync(responses[i]);
            outcomes[i] = $"{(int)responses[i].StatusCode} {detail}".Trim();
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        int rows = await db.ChallengeParticipants.CountAsync(p => p.ChallengeId == challenge.Id);
        string diagnostics = $"responses: [{string.Join("; ", outcomes)}], rows: {rows}";

        outcomes.Count(o => o == "204").Should().Be(3, diagnostics);
        outcomes.Where(o => o != "204").Should().OnlyContain(o => o == "409 Challenge.Full", diagnostics);
        rows.Should().Be(3, diagnostics);
    }

    [Fact]
    public async Task Challenges_ParallelJoinsOfOneLearner_JoinOnceAndReportAlreadyJoined()
    {
        Challenge challenge = await SeedChallengeAsync(ChallengeAudience.Adult);
        Guid adult = Guid.NewGuid();

        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            Client(adult, "Adult").PostAsync($"/api/progress/challenges/{challenge.Id}/join", null)));

        responses.Count(r => r.StatusCode == HttpStatusCode.NoContent).Should().Be(1);
        responses.Where(r => r.StatusCode != HttpStatusCode.NoContent).Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Conflict);

        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        (await db.ChallengeParticipants.CountAsync(p => p.ChallengeId == challenge.Id && p.UserId == adult)).Should().Be(1);
    }

    [Fact]
    public async Task ReviewLogRepository_GetForChallenge_LoadsWindowLogsAndEarlierLogsOfThoseWordsOnly()
    {
        Guid user = Guid.NewGuid();
        Guid seenInWindow = Guid.NewGuid();
        Guid onlyOld = Guid.NewGuid();
        DateTime start = DateTime.UtcNow.Date.AddDays(-1);
        DateTime end = start.AddDays(6);
        ReviewLog Log(Guid sense, DateTime at) => ReviewLog.Create(
            Guid.NewGuid(), user, sense, Guid.NewGuid(), at, ExerciseType.Typing, VocabularySkill.Spelling,
            isCorrect: true, responseMs: 3000, hintUsed: false, isDue: true, attemptNo: 1, FsrsRating.Good);
        ReviewLog oldOfSeen = Log(seenInWindow, start.AddDays(-30));
        ReviewLog inWindow = Log(seenInWindow, start.AddHours(2));
        ReviewLog oldOnly = Log(onlyOld, start.AddDays(-30));
        ReviewLog afterEnd = Log(seenInWindow, end.AddHours(1));
        using (IServiceScope seed = _factory.Services.CreateScope())
        {
            ProgressDbContext db = seed.ServiceProvider.GetRequiredService<ProgressDbContext>();
            db.ReviewLogs.AddRange(oldOfSeen, inWindow, oldOnly, afterEnd);
            await db.SaveChangesAsync();
        }

        using IServiceScope scope = _factory.Services.CreateScope();
        IReadOnlyList<ReviewLog> loaded = await scope.ServiceProvider.GetRequiredService<IReviewLogRepository>()
            .GetForChallengeAsync([user], start, end);

        loaded.Select(l => l.Id).Should().BeEquivalentTo([oldOfSeen.Id, inWindow.Id]);
    }

    [Fact]
    public async Task Challenges_Audiences_AreNeverMixed()
    {
        Challenge adultChallenge = await SeedChallengeAsync(ChallengeAudience.Adult);
        Challenge childChallenge = await SeedChallengeAsync(ChallengeAudience.Child);
        Guid child = Guid.NewGuid();
        await SeedConsentAsync(child, Guid.NewGuid());

        HttpResponseMessage childToAdult = await Client(child, "Child").PostAsync($"/api/progress/challenges/{adultChallenge.Id}/join", null);
        HttpResponseMessage adultToChild = await Client(Guid.NewGuid(), "Adult").PostAsync($"/api/progress/challenges/{childChallenge.Id}/join", null);
        HttpResponseMessage adultBoard = await Client(Guid.NewGuid(), "Adult").GetAsync($"/api/progress/challenges/{childChallenge.Id}/board");

        childToAdult.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemTitleAsync(childToAdult)).Should().Be("Challenge.AudienceMismatch");
        adultToChild.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        adultBoard.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        List<ChallengeDto> adultList = (await Client(Guid.NewGuid(), "Adult").GetFromJsonAsync<List<ChallengeDto>>("/api/progress/challenges", Json))!;
        adultList.Should().NotContain(c => c.Audience == ChallengeAudience.Child);
        List<ChallengeDto> childList = (await Client(child, "Child").GetFromJsonAsync<List<ChallengeDto>>("/api/progress/challenges", Json))!;
        childList.Should().NotContain(c => c.Audience == ChallengeAudience.Adult);
    }

    [Fact]
    public async Task Challenges_UnknownChallenge_Returns404()
    {
        HttpClient client = Client(Guid.NewGuid(), "Adult");

        (await client.PostAsync($"/api/progress/challenges/{Guid.NewGuid()}/join", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/progress/challenges/{Guid.NewGuid()}/board")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Challenges_Endpoints_RequireAToken()
    {
        (await _factory.CreateClient().GetAsync("/api/progress/challenges")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Challenges_ChildWithoutConsent_SeesNothingAndCannotJoinOrReadTheBoard()
    {
        Guid child = Guid.NewGuid();
        Challenge challenge = await SeedChallengeAsync(ChallengeAudience.Child);
        HttpClient client = Client(child, "Child");

        (await client.GetFromJsonAsync<List<ChallengeDto>>("/api/progress/challenges", Json))!.Should().BeEmpty();
        HttpResponseMessage join = await client.PostAsync($"/api/progress/challenges/{challenge.Id}/join", null);
        HttpResponseMessage board = await client.GetAsync($"/api/progress/challenges/{challenge.Id}/board");

        join.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemTitleAsync(join)).Should().Be("Challenge.NoConsent");
        board.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Challenges_GuardianConsent_ThenChildJoinsAndSeesOnlyOwnRankAndTopFive()
    {
        Guid guardian = Guid.NewGuid();
        Guid child = Guid.NewGuid();
        await SeedProfileAsync(guardian, AgeGroup.Adult, guardian: true);
        await SeedProfileAsync(child, AgeGroup.Child, alias: "Kiwi");
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, child, guardian);
        Challenge challenge = await SeedChallengeAsync(ChallengeAudience.Child);

        // Seven other children are already in; child i has (8 - i) counted answers, so all score above the new child.
        List<Guid> others = [];
        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
            for (int i = 1; i <= 7; i++)
            {
                Guid other = Guid.NewGuid();
                others.Add(other);
                db.ChallengeProfiles.Add(ChallengeProfile.Create(other, AgeGroup.Child, $"Pal{i}", "cat", null, null, false, DateTime.UtcNow));
                db.ChallengeParticipants.Add(ChallengeParticipant.Create(Guid.NewGuid(), challenge.Id, other, challenge.StartsAtUtc));
            }

            await db.SaveChangesAsync();
        }

        for (int i = 0; i < others.Count; i++)
        {
            await SeedReviewsAsync(others[i], counted: 7 - i);
        }

        HttpResponseMessage consent = await Client(guardian, "Adult", guardian: true)
            .PutAsJsonAsync($"/api/progress/challenges/consent/{child}", new { enabled = true });
        consent.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpClient childClient = Client(child, "Child");
        (await childClient.GetFromJsonAsync<List<ChallengeDto>>("/api/progress/challenges", Json))!.Should().Contain(c => c.Id == challenge.Id);
        (await childClient.PostAsync($"/api/progress/challenges/{challenge.Id}/join", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await RecomputeAsync();

        HttpResponseMessage response = await childClient.GetAsync($"/api/progress/challenges/{challenge.Id}/board");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string raw = await response.Content.ReadAsStringAsync();
        ChallengeBoardDto board = JsonSerializer.Deserialize<ChallengeBoardDto>(raw, Json)!;

        board.ParticipantCount.Should().Be(8);
        board.MyRank.Should().Be(8);
        board.Entries.Select(e => e.Rank).Should().Equal(1, 2, 3, 4, 5, 8);
        board.Entries.Should().HaveCount(6, "top 5 plus the own row");
        board.Entries.Count(e => e.IsMe).Should().Be(1);
        board.Entries.Single(e => e.IsMe).Name.Should().Be("Kiwi");
        board.Entries.Where(e => !e.IsMe).Should().OnlyContain(e => e.Score == null);
        raw.Should().NotContain("Real Child Name").And.NotContain(child.ToString());
        foreach (Guid other in others)
        {
            raw.Should().NotContain(other.ToString());
        }
    }

    [Fact]
    public async Task Challenges_Consent_WithoutGuardianClaimIsForbiddenByPolicy()
    {
        Guid adult = Guid.NewGuid();

        HttpResponseMessage response = await Client(adult, "Adult", guardian: false)
            .PutAsJsonAsync($"/api/progress/challenges/consent/{Guid.NewGuid()}", new { enabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Challenges_Consent_ClaimWithoutLocalGuardianProfileIsNotGuardian()
    {
        Guid supporter = Guid.NewGuid();
        Guid child = Guid.NewGuid();
        await SeedProfileAsync(supporter, AgeGroup.Adult, guardian: false);
        await SeedProfileAsync(child, AgeGroup.Child);
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, child, supporter);

        HttpResponseMessage response = await Client(supporter, "Adult", guardian: true)
            .PutAsJsonAsync($"/api/progress/challenges/consent/{child}", new { enabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemTitleAsync(response)).Should().Be("Challenge.NotGuardian");
    }

    [Fact]
    public async Task Challenges_Consent_GuardianWithoutActiveLinkIsRejected()
    {
        Guid guardian = Guid.NewGuid();
        Guid child = Guid.NewGuid();
        await SeedProfileAsync(guardian, AgeGroup.Adult, guardian: true);
        await SeedProfileAsync(child, AgeGroup.Child);

        HttpResponseMessage response = await Client(guardian, "Adult", guardian: true)
            .PutAsJsonAsync($"/api/progress/challenges/consent/{child}", new { enabled = true });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ProblemTitleAsync(response)).Should().Be("Challenge.NoActiveLink");
    }

    [Fact]
    public async Task Challenges_Consent_SwitchingOffTakesTheChildOutOfRunningChallenges()
    {
        Guid guardian = Guid.NewGuid();
        Guid child = Guid.NewGuid();
        await SeedProfileAsync(guardian, AgeGroup.Adult, guardian: true);
        await SeedProfileAsync(child, AgeGroup.Child, alias: "Kiwi");
        await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, child, guardian);
        Challenge challenge = await SeedChallengeAsync(ChallengeAudience.Child);
        HttpClient guardianClient = Client(guardian, "Adult", guardian: true);
        HttpClient childClient = Client(child, "Child");
        await guardianClient.PutAsJsonAsync($"/api/progress/challenges/consent/{child}", new { enabled = true });
        await childClient.PostAsync($"/api/progress/challenges/{challenge.Id}/join", null);

        HttpResponseMessage off = await guardianClient.PutAsJsonAsync($"/api/progress/challenges/consent/{child}", new { enabled = false });

        off.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await childClient.GetFromJsonAsync<List<ChallengeDto>>("/api/progress/challenges", Json))!.Should().BeEmpty();
        using IServiceScope scope = _factory.Services.CreateScope();
        ProgressDbContext db = scope.ServiceProvider.GetRequiredService<ProgressDbContext>();
        (await db.ChallengeParticipants.AnyAsync(p => p.ChallengeId == challenge.Id && p.UserId == child)).Should().BeFalse();
        (await db.ChallengeConsents.AnyAsync(c => c.ChildId == child)).Should().BeFalse();
    }
}
