using FluentAssertions;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;
using static WordBuddy.Progress.UnitTests.Challenges.ChallengeTestKit;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class ChallengeTests
{
    [Fact]
    public void Challenge_Lifecycle_MovesForwardThroughAllStatuses()
    {
        Challenge challenge = NewChallenge(status: ChallengeStatus.Scheduled);

        challenge.Open().IsSuccess.Should().BeTrue();
        challenge.Status.Should().Be(ChallengeStatus.Open);
        challenge.Start().IsSuccess.Should().BeTrue();
        challenge.Status.Should().Be(ChallengeStatus.Running);
        challenge.Finish().IsSuccess.Should().BeTrue();
        challenge.Status.Should().Be(ChallengeStatus.Finished);
    }

    [Theory]
    [InlineData(ChallengeStatus.Scheduled, "Start")]
    [InlineData(ChallengeStatus.Scheduled, "Finish")]
    [InlineData(ChallengeStatus.Open, "Open")]
    [InlineData(ChallengeStatus.Open, "Finish")]
    [InlineData(ChallengeStatus.Running, "Open")]
    [InlineData(ChallengeStatus.Running, "Start")]
    [InlineData(ChallengeStatus.Finished, "Open")]
    [InlineData(ChallengeStatus.Finished, "Start")]
    [InlineData(ChallengeStatus.Finished, "Finish")]
    public void Challenge_Lifecycle_InvalidTransitionFailsAndKeepsStatus(ChallengeStatus from, string action)
    {
        Challenge challenge = NewChallenge(status: from);

        Result result = action switch
        {
            "Open" => challenge.Open(),
            "Start" => challenge.Start(),
            _ => challenge.Finish(),
        };

        result.Error.Should().Be(ChallengeErrors.InvalidTransition);
        challenge.Status.Should().Be(from);
    }

    [Theory]
    [InlineData(0, 30)] // ends when it starts
    [InlineData(7, 0)] // no capacity
    public void Challenge_Create_RejectsBadScheduleOrCapacity(int lengthDays, int capacity)
    {
        Result<Challenge> result = Challenge.Create(
            Guid.NewGuid(), "weekly-league", ChallengeAudience.Adult, ChallengeKind.Ranked, ChallengeMetric.MemoryPoints, null,
            Monday.AddDays(-1), Monday, Monday.AddDays(lengthDays), capacity);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ChallengeErrors.InvalidSchedule);
    }

    [Fact]
    public void Challenge_Create_TargetKindNeedsTargetAndRankedMustNotHaveOne()
    {
        Result<Challenge> targetWithout = Challenge.Create(
            Guid.NewGuid(), "x", ChallengeAudience.Adult, ChallengeKind.Target, ChallengeMetric.RecallUpgrades, null,
            Monday.AddDays(-1), Monday, Monday.AddDays(7), 30);
        Result<Challenge> rankedWith = Challenge.Create(
            Guid.NewGuid(), "x", ChallengeAudience.Adult, ChallengeKind.Ranked, ChallengeMetric.MemoryPoints, 5,
            Monday.AddDays(-1), Monday, Monday.AddDays(7), 30);

        targetWithout.Error.Should().Be(ChallengeErrors.InvalidSchedule);
        rankedWith.Error.Should().Be(ChallengeErrors.InvalidSchedule);
    }

    [Theory]
    [InlineData(ChallengeStatus.Open)]
    [InlineData(ChallengeStatus.Running)]
    public void Challenge_CheckJoinable_OpenOrRunningSameAudienceIsOk(ChallengeStatus status)
    {
        NewChallenge(status: status).CheckJoinable(ChallengeAudience.Adult).IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(ChallengeStatus.Scheduled)]
    [InlineData(ChallengeStatus.Finished)]
    public void Challenge_CheckJoinable_ScheduledOrFinishedIsNotOpen(ChallengeStatus status)
    {
        NewChallenge(status: status).CheckJoinable(ChallengeAudience.Adult).Error.Should().Be(ChallengeErrors.NotOpen);
    }

    [Fact]
    public void Challenge_CheckJoinable_OtherAudienceIsMismatch()
    {
        NewChallenge(ChallengeAudience.Adult).CheckJoinable(ChallengeAudience.Child).Error.Should().Be(ChallengeErrors.AudienceMismatch);
        NewChallenge(ChallengeAudience.Child).CheckJoinable(ChallengeAudience.Adult).Error.Should().Be(ChallengeErrors.AudienceMismatch);
    }

    [Fact]
    public void Challenge_HasRoom_FalseAtCapacity()
    {
        Challenge challenge = NewChallenge(capacity: 2);

        challenge.HasRoom(1).Should().BeTrue();
        challenge.HasRoom(2).Should().BeFalse();
    }

    [Fact]
    public void ChallengeProfile_Apply_ChildNeverKeepsDisplayNameAndOlderEventIsIgnored()
    {
        ChallengeProfile profile = ChallengeProfile.Create(
            Guid.NewGuid(), WordBuddy.Progress.Domain.AgeGroup.Child, null, "owl", "Real Name", null, false, Monday);

        profile.DisplayName.Should().BeNull();
        profile.PublicName.Should().Be(ChallengeProfile.ChildFallbackName);
        profile.Apply(WordBuddy.Progress.Domain.AgeGroup.Child, "Fox", "fox", null, null, false, Monday.AddMinutes(-1)).Should().BeFalse();
        profile.Alias.Should().BeNull();
        profile.Apply(WordBuddy.Progress.Domain.AgeGroup.Child, "Fox", "fox", null, null, false, Monday.AddMinutes(1)).Should().BeTrue();
        profile.PublicName.Should().Be("Fox");
    }
}
