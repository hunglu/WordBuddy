using FluentAssertions;
using WordBuddy.Progress.Domain.Challenges;

namespace WordBuddy.Progress.UnitTests.Challenges;

public class LearnerDayResolverTests
{
    [Fact]
    public void LearnerDayResolver_ToLocalDay_ZoneAheadOfUtcMovesPastMidnight()
    {
        DateTime utc = new(2026, 10, 6, 17, 30, 0, DateTimeKind.Utc);

        LearnerDayResolver.ToLocalDay(utc, "Asia/Ho_Chi_Minh").Should().Be(new DateOnly(2026, 10, 7));
    }

    [Fact]
    public void LearnerDayResolver_ToLocalDay_ZoneBehindUtcStaysOnThePreviousDay()
    {
        DateTime utc = new(2026, 10, 7, 2, 30, 0, DateTimeKind.Utc);

        LearnerDayResolver.ToLocalDay(utc, "America/New_York").Should().Be(new DateOnly(2026, 10, 6));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Mars/Olympus_Mons")]
    public void LearnerDayResolver_ToLocalDay_NullBlankOrUnknownZoneFallsBackToUtc(string? zone)
    {
        DateTime utc = new(2026, 10, 6, 23, 59, 0, DateTimeKind.Utc);

        LearnerDayResolver.ToLocalDay(utc, zone).Should().Be(new DateOnly(2026, 10, 6));
    }

    [Fact]
    public void LearnerDayResolver_ToLocalDay_IgnoresTheKindOfTheInput()
    {
        DateTime unspecified = new(2026, 10, 6, 17, 30, 0, DateTimeKind.Unspecified);

        LearnerDayResolver.ToLocalDay(unspecified, "Asia/Ho_Chi_Minh").Should().Be(new DateOnly(2026, 10, 7));
    }
}
