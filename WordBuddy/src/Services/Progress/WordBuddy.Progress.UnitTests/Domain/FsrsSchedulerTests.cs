using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

/// <summary>Reference vectors produced by py-fsrs 6.3.2 (<c>Scheduler(enable_fuzzing=False)</c>,
/// default parameters, retention 0.9, steps 1m/10m, relearning 10m), starting at T0.</summary>
public class FsrsSchedulerTests
{
    private const double Precision = 1e-9;
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly FsrsScheduler _scheduler = new(new VocabularySchedulingOptions());

    [Theory]
    [InlineData(FsrsRating.Again, FsrsPhase.Learning, 0, 0.212, 6.4133, 1.0)]
    [InlineData(FsrsRating.Hard, FsrsPhase.Learning, 0, 1.2931, 5.112170705601056, 5.5)]
    [InlineData(FsrsRating.Good, FsrsPhase.Learning, 1, 2.3065, 2.118103970459016, 10.0)]
    [InlineData(FsrsRating.Easy, FsrsPhase.Review, null, 8.2956, 1.0, 8 * 24 * 60.0)]
    public void FsrsScheduler_Schedule_FirstReviewMatchesReference(
        FsrsRating rating, FsrsPhase phase, int? step, double stability, double difficulty, double dueAfterMinutes)
    {
        FsrsCard card = _scheduler.Schedule(FsrsCard.New(T0), rating, T0);

        card.Phase.Should().Be(phase);
        card.Step.Should().Be(step);
        card.Stability.Should().BeApproximately(stability, Precision);
        card.Difficulty.Should().BeApproximately(difficulty, Precision);
        card.DueAtUtc.Should().Be(T0.AddMinutes(dueAfterMinutes));
        card.LastReviewedAtUtc.Should().Be(T0);
    }

    [Fact]
    public void FsrsScheduler_Schedule_MultiStepSequenceWithLapseMatchesReference()
    {
        (FsrsRating Rating, FsrsPhase Phase, int? Step, double Stability, double Difficulty, DateTime Due)[] expected =
        [
            (FsrsRating.Good, FsrsPhase.Learning, 1, 2.3065, 2.118103970459016, new DateTime(2026, 1, 1, 0, 10, 0, DateTimeKind.Utc)),
            (FsrsRating.Good, FsrsPhase.Review, null, 2.3065, 2.111214235785395, new DateTime(2026, 1, 3, 0, 10, 0, DateTimeKind.Utc)),
            (FsrsRating.Good, FsrsPhase.Review, null, 10.971048263078135, 2.1043313908464483, new DateTime(2026, 1, 14, 0, 10, 0, DateTimeKind.Utc)),
            (FsrsRating.Good, FsrsPhase.Review, null, 46.316858440073425, 2.0974554287524403, new DateTime(2026, 3, 1, 0, 10, 0, DateTimeKind.Utc)),
            (FsrsRating.Again, FsrsPhase.Relearning, 0, 2.9338452901880046, 7.387715706030851, new DateTime(2026, 3, 1, 0, 20, 0, DateTimeKind.Utc)),
            (FsrsRating.Good, FsrsPhase.Review, null, 2.9338452901880046, 7.375556359621658, new DateTime(2026, 3, 4, 0, 20, 0, DateTimeKind.Utc)),
            (FsrsRating.Hard, FsrsPhase.Review, null, 5.85981081282548, 8.243000381740053, new DateTime(2026, 3, 10, 0, 20, 0, DateTimeKind.Utc)),
            (FsrsRating.Easy, FsrsPhase.Review, null, 18.20855057710452, 7.641121354796629, new DateTime(2026, 3, 28, 0, 20, 0, DateTimeKind.Utc)),
        ];

        FsrsCard card = FsrsCard.New(T0);
        DateTime reviewAt = T0;

        foreach ((FsrsRating rating, FsrsPhase phase, int? step, double stability, double difficulty, DateTime due) in expected)
        {
            card = _scheduler.Schedule(card, rating, reviewAt);

            card.Phase.Should().Be(phase);
            card.Step.Should().Be(step);
            card.Stability.Should().BeApproximately(stability, Precision);
            card.Difficulty.Should().BeApproximately(difficulty, Precision);
            card.DueAtUtc.Should().Be(due);
            reviewAt = card.DueAtUtc;
        }
    }

    [Fact]
    public void FsrsScheduler_Schedule_LateReviewUsesRetrievability()
    {
        FsrsCard card = _scheduler.Schedule(FsrsCard.New(T0), FsrsRating.Easy, T0);
        card = _scheduler.Schedule(card, FsrsRating.Good, card.DueAtUtc.AddDays(10));

        card.Phase.Should().Be(FsrsPhase.Review);
        card.Stability.Should().BeApproximately(60.2179310092579, Precision);
        card.Difficulty.Should().BeApproximately(1.0, Precision);
        card.DueAtUtc.Should().Be(new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void FsrsScheduler_Schedule_IsDeterministic()
    {
        FsrsCard first = _scheduler.Schedule(FsrsCard.New(T0), FsrsRating.Easy, T0);
        FsrsCard second = _scheduler.Schedule(FsrsCard.New(T0), FsrsRating.Easy, T0);

        second.Should().Be(first);
    }
}
