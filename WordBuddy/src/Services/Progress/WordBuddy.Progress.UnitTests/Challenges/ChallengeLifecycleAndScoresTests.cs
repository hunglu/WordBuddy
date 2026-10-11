using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Features.Challenges;
using WordBuddy.Progress.Application.Features.Challenges.Commands.AdvanceChallengeLifecycle;
using WordBuddy.Progress.Application.Features.Challenges.Commands.RecomputeChallengeScores;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Progress.Infrastructure.Challenges;
using WordBuddy.Shared.Kernel;
using static WordBuddy.Progress.UnitTests.Challenges.ChallengeTestKit;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class ChallengeLifecycleAndScoresTests
{
    private static readonly DateTime Wednesday = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeChallengeRepository _repo = new();
    private readonly FakeChallengeProfileRepository _profiles = new();
    private readonly FakeDistributedCache _cache = new();
    private readonly Mock<IReviewLogRepository> _reviewLogs = new();
    private readonly FixedTimeProvider _time = new(Wednesday);
    private readonly ChallengeOptions _options = new();
    private readonly List<ReviewLog> _logs = [];

    public ChallengeLifecycleAndScoresTests()
    {
        _reviewLogs.Setup(r => r.GetByUsersAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => (IReadOnlyList<ReviewLog>)[.. _logs.Where(l => ids.Contains(l.UserId))]);
    }

    private ChallengeScoreProjector Projector() =>
        new(_repo, _profiles, _reviewLogs.Object, _options, Mock.Of<ILogger<ChallengeScoreProjector>>());

    private RecomputeChallengeScoresCommandHandler Recompute() =>
        new(_repo, Projector(), _cache, Mock.Of<ILogger<RecomputeChallengeScoresCommandHandler>>());

    private AdvanceChallengeLifecycleCommandHandler Advance() =>
        new(_repo, Projector(), _cache, _options, _time, Mock.Of<ILogger<AdvanceChallengeLifecycleCommandHandler>>());

    /// <summary>A learner who joined at <paramref name="joined"/> and has <paramref name="countedAnswers"/> counted answers on distinct days.</summary>
    private Guid Learner(Challenge challenge, int countedAnswers, DateTime? joined = null)
    {
        Guid user = Guid.NewGuid();
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), challenge.Id, user, joined ?? challenge.StartsAtUtc));
        for (int i = 0; i < countedAnswers; i++)
        {
            Guid sense = Guid.NewGuid();
            _logs.Add(Log(user, sense, challenge.StartsAtUtc.AddDays(-1)));
            _logs.Add(Log(user, sense, challenge.StartsAtUtc.AddDays(1).AddHours(i)));
        }

        return user;
    }

    private ChallengeParticipant Participant(Challenge challenge, Guid user) =>
        _repo.Participants.Single(p => p.ChallengeId == challenge.Id && p.UserId == user);

    [Fact]
    public async Task RecomputeChallengeScoresCommandHandler_HandleAsync_WritesScoresAndDenseRanks()
    {
        Challenge challenge = NewChallenge(status: ChallengeStatus.Running);
        _repo.Challenges.Add(challenge);
        Guid top = Learner(challenge, 3);
        Guid tieA = Learner(challenge, 2);
        Guid tieB = Learner(challenge, 2);
        Guid none = Learner(challenge, 0);

        Result<int> result = await Recompute().HandleAsync(new RecomputeChallengeScoresCommand());

        result.Value.Should().Be(1);
        Participant(challenge, top).Score.Should().Be(30);
        Participant(challenge, top).Rank.Should().Be(1);
        Participant(challenge, tieA).Rank.Should().Be(2);
        Participant(challenge, tieB).Rank.Should().Be(2);
        Participant(challenge, none).Score.Should().Be(0);
        Participant(challenge, none).Rank.Should().Be(3, "dense rank: no gap after a tie");
    }

    [Fact]
    public async Task RecomputeChallengeScoresCommandHandler_HandleAsync_RunningTwiceGivesTheSameState()
    {
        Challenge challenge = NewChallenge(status: ChallengeStatus.Running);
        _repo.Challenges.Add(challenge);
        Guid user = Learner(challenge, 4);
        Learner(challenge, 1);

        await Recompute().HandleAsync(new RecomputeChallengeScoresCommand());
        (int score, int? rank) first = (Participant(challenge, user).Score, Participant(challenge, user).Rank);
        await Recompute().HandleAsync(new RecomputeChallengeScoresCommand());

        (Participant(challenge, user).Score, Participant(challenge, user).Rank).Should().Be(first);
    }

    [Fact]
    public async Task RecomputeChallengeScoresCommandHandler_HandleAsync_OnlyRunningChallengesAreTouchedAndCacheIsDeleted()
    {
        Challenge running = NewChallenge(status: ChallengeStatus.Running);
        Challenge open = NewChallenge(status: ChallengeStatus.Open);
        _repo.Challenges.AddRange([running, open]);
        Guid runner = Learner(running, 2);
        Guid waiter = Learner(open, 2);
        _cache.Items[ChallengeCache.BoardKey(running.Id)] = [1];
        _cache.Items[ChallengeCache.BoardKey(open.Id)] = [1];

        await Recompute().HandleAsync(new RecomputeChallengeScoresCommand());

        Participant(running, runner).Rank.Should().NotBeNull();
        Participant(open, waiter).Rank.Should().BeNull();
        _cache.Items.Should().NotContainKey(ChallengeCache.BoardKey(running.Id));
        _cache.Items.Should().ContainKey(ChallengeCache.BoardKey(open.Id));
    }

    [Fact]
    public async Task RecomputeChallengeScoresCommandHandler_HandleAsync_UsesTheTimeZoneOfEachProfile()
    {
        Challenge challenge = NewChallenge(status: ChallengeStatus.Running);
        _repo.Challenges.Add(challenge);
        _options.DailyPointCap = 15;
        Guid hcm = Guid.NewGuid();
        Guid newYork = Guid.NewGuid();
        _profiles.Profiles[hcm] = Profile(hcm, AgeGroup.Adult, timeZoneId: "Asia/Ho_Chi_Minh");
        _profiles.Profiles[newYork] = Profile(newYork, AgeGroup.Adult, timeZoneId: "America/New_York");
        foreach (Guid user in new[] { hcm, newYork })
        {
            _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), challenge.Id, user, challenge.StartsAtUtc));
            Guid a = Guid.NewGuid();
            Guid b = Guid.NewGuid();
            _logs.Add(Log(user, a, challenge.StartsAtUtc.AddDays(-1)));
            _logs.Add(Log(user, b, challenge.StartsAtUtc.AddDays(-1)));
            _logs.Add(Log(user, a, new DateTime(2026, 10, 6, 16, 30, 0, DateTimeKind.Utc)));
            _logs.Add(Log(user, b, new DateTime(2026, 10, 6, 17, 30, 0, DateTimeKind.Utc)));
        }

        await Recompute().HandleAsync(new RecomputeChallengeScoresCommand());

        Participant(challenge, hcm).Score.Should().Be(20, "two local days under the cap");
        Participant(challenge, newYork).Score.Should().Be(15, "one local day, capped");
    }

    [Fact]
    public async Task RecomputeChallengeScoresCommandHandler_HandleAsync_TargetReachedIsSetAtTheTarget()
    {
        Challenge challenge = NewChallenge(kind: ChallengeKind.Target, metric: ChallengeMetric.DueReviewsCorrect, target: 2, status: ChallengeStatus.Running);
        _repo.Challenges.Add(challenge);
        Guid user = Guid.NewGuid();
        _repo.Participants.Add(ChallengeParticipant.Create(Guid.NewGuid(), challenge.Id, user, challenge.StartsAtUtc));
        Guid sense = Guid.NewGuid();
        _logs.Add(Log(user, sense, challenge.StartsAtUtc.AddDays(-1)));
        _logs.Add(Log(user, sense, challenge.StartsAtUtc.AddDays(1)));
        await Recompute().HandleAsync(new RecomputeChallengeScoresCommand());
        Participant(challenge, user).TargetReached.Should().BeFalse();

        _logs.Add(Log(user, sense, challenge.StartsAtUtc.AddDays(2)));
        await Recompute().HandleAsync(new RecomputeChallengeScoresCommand());

        Participant(challenge, user).Score.Should().Be(2);
        Participant(challenge, user).TargetReached.Should().BeTrue();
    }

    [Fact]
    public async Task AdvanceChallengeLifecycleCommandHandler_HandleAsync_FirstRunCreatesSevenRunningAndSevenUpcoming()
    {
        Result<int> result = await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());

        result.IsSuccess.Should().BeTrue();
        _repo.Challenges.Should().HaveCount(14);
        _repo.Challenges.Count(c => c.Status == ChallengeStatus.Running).Should().Be(7);
        _repo.Challenges.Count(c => c.Status == ChallengeStatus.Scheduled).Should().Be(7);
        _repo.Challenges.Should().NotContain(c => c.TemplateKey == ChallengeTemplates.ListeningSprintKey && c.Audience == ChallengeAudience.Child);
        _repo.Challenges.Where(c => c.TemplateKey == ChallengeTemplates.WeeklyLeagueKey).Select(c => c.Audience).Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task AdvanceChallengeLifecycleCommandHandler_HandleAsync_RerunChangesNothingAndNeverAddsOverflowInstances()
    {
        await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());

        Result<int> second = await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());
        Result<int> third = await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());

        second.Value.Should().Be(0);
        third.Value.Should().Be(0);
        _repo.Challenges.Should().HaveCount(14);
        _repo.Challenges.GroupBy(c => (c.TemplateKey, c.Audience))
            .Should().OnlyContain(g => g.Count(c => c.StartsAtUtc > Wednesday) == 1, "exactly one upcoming instance per template and audience");
    }

    [Fact]
    public async Task AdvanceChallengeLifecycleCommandHandler_HandleAsync_TimePassingOpensStartsAndFinishesInOrder()
    {
        await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());
        Challenge nextWeekly = _repo.Challenges.Single(c => c.TemplateKey == ChallengeTemplates.WeeklyLeagueKey && c.Audience == ChallengeAudience.Adult && c.StartsAtUtc > Wednesday);

        _time.UtcNow = new DateTime(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);
        await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());
        nextWeekly.Status.Should().Be(ChallengeStatus.Open, "joining opens 24 hours before the start");

        _time.UtcNow = new DateTime(2026, 10, 12, 0, 0, 1, DateTimeKind.Utc);
        await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());
        nextWeekly.Status.Should().Be(ChallengeStatus.Running);
        _repo.Challenges.Single(c => c.TemplateKey == ChallengeTemplates.WeeklyLeagueKey && c.Audience == ChallengeAudience.Adult && c.StartsAtUtc == Monday)
            .Status.Should().Be(ChallengeStatus.Finished);
        _repo.Challenges.Count(c => c.TemplateKey == ChallengeTemplates.WeeklyLeagueKey && c.Audience == ChallengeAudience.Adult && c.Status != ChallengeStatus.Finished)
            .Should().Be(2, "the running week and one upcoming week");
    }

    [Fact]
    public async Task AdvanceChallengeLifecycleCommandHandler_HandleAsync_ScoresAreProjectedBeforeAChallengeFinishes()
    {
        await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());
        Challenge weekly = _repo.Challenges.Single(c => c.TemplateKey == ChallengeTemplates.WeeklyLeagueKey && c.Audience == ChallengeAudience.Adult && c.StartsAtUtc == Monday);
        Guid user = Learner(weekly, 3);
        _cache.Items[ChallengeCache.BoardKey(weekly.Id)] = [1];

        _time.UtcNow = new DateTime(2026, 10, 12, 0, 0, 1, DateTimeKind.Utc);
        await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());

        weekly.Status.Should().Be(ChallengeStatus.Finished);
        Participant(weekly, user).Score.Should().Be(30);
        Participant(weekly, user).Rank.Should().Be(1);
        _cache.Items.Should().NotContainKey(ChallengeCache.BoardKey(weekly.Id));
    }

    [Fact]
    public async Task AdvanceChallengeLifecycleCommandHandler_HandleAsync_SchedulerDownForWeeksCatchesUpToOneUpcomingInstance()
    {
        await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());

        _time.UtcNow = new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);
        await Advance().HandleAsync(new AdvanceChallengeLifecycleCommand());

        _repo.Challenges.GroupBy(c => (c.TemplateKey, c.Audience))
            .Should().OnlyContain(g => g.Count(c => c.StartsAtUtc > _time.UtcNow) == 1);
        _repo.Challenges.Where(c => c.Status == ChallengeStatus.Running).Should().OnlyContain(c => c.StartsAtUtc <= _time.UtcNow && c.EndsAtUtc > _time.UtcNow);
    }

    [Fact]
    public async Task ChallengeSchedulerHostedService_RunTickAsync_SurvivesAFailingHandlerAndRecomputesOnItsOwnInterval()
    {
        Mock<ICommandHandler<AdvanceChallengeLifecycleCommand, int>> advance = new();
        advance.Setup(h => h.HandleAsync(It.IsAny<AdvanceChallengeLifecycleCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database not ready"));
        Mock<ICommandHandler<RecomputeChallengeScoresCommand, int>> recompute = new();
        recompute.Setup(h => h.HandleAsync(It.IsAny<RecomputeChallengeScoresCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(0));
        ServiceCollection services = new();
        services.AddSingleton(advance.Object);
        services.AddSingleton(recompute.Object);
        using ServiceProvider provider = services.BuildServiceProvider();
        ChallengeSchedulerHostedService scheduler = new(
            provider.GetRequiredService<IServiceScopeFactory>(), _options, _time, Mock.Of<ILogger<ChallengeSchedulerHostedService>>());

        await scheduler.RunTickAsync(CancellationToken.None);
        await scheduler.RunTickAsync(CancellationToken.None);
        _time.UtcNow = _time.UtcNow.AddMinutes(6);
        await scheduler.RunTickAsync(CancellationToken.None);

        advance.Verify(h => h.HandleAsync(It.IsAny<AdvanceChallengeLifecycleCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        recompute.Verify(h => h.HandleAsync(It.IsAny<RecomputeChallengeScoresCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
