using FluentAssertions;
using WordBuddy.Progress.Domain.Challenges;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class ChallengeTemplatesTests
{
    private static readonly TimeSpan Lead = TimeSpan.FromHours(24);

    [Fact]
    public void ChallengeTemplates_All_HasTheFourLaunchTemplates()
    {
        ChallengeTemplates.All.Select(t => t.Key).Should().BeEquivalentTo(
            [ChallengeTemplates.WeeklyLeagueKey, ChallengeTemplates.SevenDayMemoryKey, ChallengeTemplates.WordClimberKey, ChallengeTemplates.ListeningSprintKey]);
    }

    [Fact]
    public void ChallengeTemplates_ListeningSprint_IsAdultOnly()
    {
        ChallengeTemplate sprint = ChallengeTemplates.Find(ChallengeTemplates.ListeningSprintKey)!;

        sprint.Audiences.Should().Equal(ChallengeAudience.Adult);
        sprint.Audiences.Should().NotContain(ChallengeAudience.Child);
    }

    [Fact]
    public void ChallengeTemplates_OtherTemplates_HaveAdultAndChildAsSeparateAudiences()
    {
        foreach (ChallengeTemplate template in ChallengeTemplates.All.Where(t => t.Key != ChallengeTemplates.ListeningSprintKey))
        {
            template.Audiences.Should().BeEquivalentTo([ChallengeAudience.Adult, ChallengeAudience.Child]);
            template.Audiences.Should().OnlyHaveUniqueItems();
        }
    }

    [Fact]
    public void ChallengeTemplates_Targets_MatchTheLaunchSet()
    {
        ChallengeTemplates.Find(ChallengeTemplates.SevenDayMemoryKey)!.Target.Should().Be(7);
        ChallengeTemplates.Find(ChallengeTemplates.WordClimberKey)!.Target.Should().Be(20);
        ChallengeTemplates.Find(ChallengeTemplates.WeeklyLeagueKey)!.Target.Should().BeNull();
        ChallengeTemplates.NameOf("unknown-key").Should().Be("unknown-key");
    }

    [Fact]
    public void ChallengeTemplate_NextWindows_NoInstanceYet_WeeklyStartsOnTheMondayOfThisWeekAndQueuesNextWeek()
    {
        ChallengeTemplate weekly = ChallengeTemplates.Find(ChallengeTemplates.WeeklyLeagueKey)!;
        DateTime now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

        IReadOnlyList<ChallengeWindow> windows = weekly.NextWindows(null, null, now, Lead);

        windows.Should().HaveCount(2, "the running week and the next week");
        windows[0].StartsAtUtc.Should().Be(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));
        windows[0].EndsAtUtc.Should().Be(new DateTime(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc));
        windows[0].OpensAtUtc.Should().Be(windows[0].StartsAtUtc - Lead);
        windows[1].StartsAtUtc.Should().Be(windows[0].EndsAtUtc);
        windows[1].StartsAtUtc.Should().BeAfter(now);
    }

    [Fact]
    public void ChallengeTemplate_NextWindows_NoInstanceYet_OtherTemplatesStartToday()
    {
        ChallengeTemplate climber = ChallengeTemplates.Find(ChallengeTemplates.WordClimberKey)!;
        DateTime now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

        IReadOnlyList<ChallengeWindow> windows = climber.NextWindows(null, null, now, Lead);

        windows[0].StartsAtUtc.Should().Be(new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc));
        windows[0].EndsAtUtc.Should().Be(windows[0].StartsAtUtc.AddDays(14));
        windows.Count(w => w.StartsAtUtc > now).Should().Be(1, "never more than one future instance");
    }

    [Fact]
    public void ChallengeTemplate_NextWindows_LatestNotStartedYet_CreatesNothing()
    {
        ChallengeTemplate weekly = ChallengeTemplates.Find(ChallengeTemplates.WeeklyLeagueKey)!;
        DateTime now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

        weekly.NextWindows(now.AddDays(3), now.AddDays(10), now, Lead).Should().BeEmpty();
    }

    [Fact]
    public void ChallengeTemplate_NextWindows_LatestRunning_CreatesOneFutureWindowRightAfterIt()
    {
        ChallengeTemplate weekly = ChallengeTemplates.Find(ChallengeTemplates.WeeklyLeagueKey)!;
        DateTime now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
        DateTime start = new(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);

        IReadOnlyList<ChallengeWindow> windows = weekly.NextWindows(start, start.AddDays(7), now, Lead);

        windows.Should().ContainSingle();
        windows[0].StartsAtUtc.Should().Be(start.AddDays(7));
    }

    [Fact]
    public void ChallengeTemplate_NextWindows_SchedulerWasDown_CatchesUpAndEndsWithOneFutureWindow()
    {
        ChallengeTemplate listening = ChallengeTemplates.Find(ChallengeTemplates.ListeningSprintKey)!;
        DateTime start = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime now = new(2026, 10, 11, 12, 0, 0, DateTimeKind.Utc);

        IReadOnlyList<ChallengeWindow> windows = listening.NextWindows(start, start.AddDays(3), now, Lead);

        windows.Count(w => w.StartsAtUtc > now).Should().Be(1);
        windows.Last().StartsAtUtc.Should().BeAfter(now);
        windows.Zip(windows.Skip(1), (a, b) => a.EndsAtUtc == b.StartsAtUtc).Should().OnlyContain(contiguous => contiguous);
    }
}
