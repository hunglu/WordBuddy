using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

public class LearnerWordStateTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly VocabularySchedulingOptions _options = new();
    private readonly FsrsScheduler _scheduler = new(new VocabularySchedulingOptions());

    private static LearnerWordState NewState() => LearnerWordState.CreateNew(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0);

    [Fact]
    public void LearnerWordState_CreateNew_IsActiveNewAndDueAtAddTime()
    {
        LearnerWordState state = NewState();

        state.Status.Should().Be(WordStatus.New);
        state.IsActive.Should().BeTrue();
        state.DueAtUtc.Should().Be(T0);
        state.Reps.Should().Be(0);
        state.FirstReviewedAtUtc.Should().BeNull();
    }

    [Fact]
    public void LearnerWordState_ApplyReview_NewToLearningToReviewToMastered()
    {
        LearnerWordState state = NewState();

        state.ApplyReview(FsrsRating.Good, T0, _scheduler, _options);
        state.Status.Should().Be(WordStatus.Learning);
        state.FirstReviewedAtUtc.Should().Be(T0);

        state.ApplyReview(FsrsRating.Good, state.DueAtUtc, _scheduler, _options);
        state.Status.Should().Be(WordStatus.Review);

        // Stability: 10.97 → 46.3 days crosses the 21-day mastered threshold.
        state.ApplyReview(FsrsRating.Good, state.DueAtUtc, _scheduler, _options);
        state.Status.Should().Be(WordStatus.Review);
        state.ApplyReview(FsrsRating.Good, state.DueAtUtc, _scheduler, _options);
        state.Status.Should().Be(WordStatus.Mastered);

        state.Reps.Should().Be(4);
        state.FirstReviewedAtUtc.Should().Be(T0);
    }

    [Fact]
    public void LearnerWordState_ApplyReview_LeechAtFourLapsesAndStaysScheduled()
    {
        LearnerWordState state = NewState();
        state.ApplyReview(FsrsRating.Easy, T0, _scheduler, _options);

        for (int lapse = 1; lapse <= 4; lapse++)
        {
            state.ApplyReview(FsrsRating.Again, state.DueAtUtc, _scheduler, _options);
            state.Lapses.Should().Be(lapse);
            state.Status.Should().Be(lapse < 4 ? WordStatus.Learning : WordStatus.Leech);

            // Relearning step done → back to Review (status stays Leech once reached).
            state.ApplyReview(FsrsRating.Good, state.DueAtUtc, _scheduler, _options);
        }

        state.Status.Should().Be(WordStatus.Leech);
        state.DueAtUtc.Should().BeAfter(T0);
        state.FsrsPhase.Should().Be(FsrsPhase.Review);
    }

    [Fact]
    public void LearnerWordState_ApplyReview_AgainInLearningIsNotALapse()
    {
        LearnerWordState state = NewState();

        state.ApplyReview(FsrsRating.Again, T0, _scheduler, _options);

        state.Lapses.Should().Be(0);
        state.Status.Should().Be(WordStatus.Learning);
    }

    [Fact]
    public void LearnerWordState_DeactivateThenActivate_KeepsFsrsData()
    {
        LearnerWordState state = NewState();
        state.ApplyReview(FsrsRating.Easy, T0, _scheduler, _options);
        double stability = state.Stability;
        DateTime due = state.DueAtUtc;

        state.Deactivate();
        state.IsActive.Should().BeFalse();
        state.Activate();

        state.IsActive.Should().BeTrue();
        state.Stability.Should().Be(stability);
        state.DueAtUtc.Should().Be(due);
        state.Reps.Should().Be(1);
        state.Status.Should().Be(WordStatus.Review);
    }
}
