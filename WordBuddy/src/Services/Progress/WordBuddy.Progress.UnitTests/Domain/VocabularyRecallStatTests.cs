using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

public class VocabularyRecallStatTests
{
    private static VocabularyRecallStat CreateStat() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "apple");

    [Fact]
    public void ApplyCheckResult_Known_SetsStatusKnownAndIncrementsCounts()
    {
        VocabularyRecallStat stat = CreateStat();

        stat.ApplyCheckResult(true);

        stat.Status.Should().Be(RecallStatus.Known);
        stat.TimesChecked.Should().Be(1);
        stat.TimesKnown.Should().Be(1);
    }

    [Fact]
    public void ApplyCheckResult_NotKnown_SetsStatusLearningAndIncrementsOnlyTimesChecked()
    {
        VocabularyRecallStat stat = CreateStat();

        stat.ApplyCheckResult(false);

        stat.Status.Should().Be(RecallStatus.Learning);
        stat.TimesChecked.Should().Be(1);
        stat.TimesKnown.Should().Be(0);
    }

    [Fact]
    public void ApplyCheckResult_LastCheckWins_MostRecentCheckDeterminesStatus()
    {
        VocabularyRecallStat stat = CreateStat();

        stat.ApplyCheckResult(true);
        stat.ApplyCheckResult(false);

        stat.Status.Should().Be(RecallStatus.Learning);
        stat.TimesChecked.Should().Be(2);
        stat.TimesKnown.Should().Be(1);
    }

    [Fact]
    public void ApplyCheckResult_KnownAfterLearning_FlipsStatusBackToKnown()
    {
        VocabularyRecallStat stat = CreateStat();

        stat.ApplyCheckResult(false);
        stat.ApplyCheckResult(true);

        stat.Status.Should().Be(RecallStatus.Known);
        stat.TimesChecked.Should().Be(2);
        stat.TimesKnown.Should().Be(1);
    }
}
