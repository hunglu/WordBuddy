using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

/// <summary>WB-20: a known age band wins over the age group multiplier; no band keeps the WB-22 behaviour.</summary>
public class AnswerGraderAgeBandTests
{
    private readonly AnswerGrader _grader = new(new VocabularyGradingOptions());

    [Theory]
    // PictureChoice base 3000 / 10000. Fast boundary = 3000 x multiplier.
    [InlineData(AgeBand.Under7, 4500, FsrsRating.Easy)]
    [InlineData(AgeBand.Under7, 4501, FsrsRating.Good)]
    [InlineData(AgeBand.Age7To9, 4050, FsrsRating.Easy)]
    [InlineData(AgeBand.Age7To9, 4051, FsrsRating.Good)]
    [InlineData(AgeBand.Age10To12, 3600, FsrsRating.Easy)]
    [InlineData(AgeBand.Age10To12, 3601, FsrsRating.Good)]
    [InlineData(AgeBand.Age13To17, 3300, FsrsRating.Easy)]
    [InlineData(AgeBand.Age13To17, 3301, FsrsRating.Good)]
    [InlineData(AgeBand.Adult, 3000, FsrsRating.Easy)]
    [InlineData(AgeBand.Adult, 3001, FsrsRating.Good)]
    public void AnswerGrader_Grade_BandScalesFastThreshold(AgeBand band, int responseMs, FsrsRating expected)
    {
        _grader.Grade(ExerciseType.PictureChoice, AgeGroup.Child, isCorrect: true, responseMs, hintUsed: false, band)
            .Should().Be(expected);
    }

    [Fact]
    public void AnswerGrader_Grade_BandWinsOverAgeGroup()
    {
        // Child group would allow 3750 ms for Easy; the Adult band allows only 3000 ms.
        _grader.Grade(ExerciseType.PictureChoice, AgeGroup.Child, isCorrect: true, responseMs: 3750, hintUsed: false, AgeBand.Adult)
            .Should().Be(FsrsRating.Good);
    }

    [Theory]
    [InlineData(AgeGroup.Child, 3750, FsrsRating.Easy)]
    [InlineData(AgeGroup.Child, 3751, FsrsRating.Good)]
    [InlineData(AgeGroup.Adult, 3000, FsrsRating.Easy)]
    [InlineData(AgeGroup.Adult, 3001, FsrsRating.Good)]
    public void AnswerGrader_Grade_NoBandFallsBackToAgeGroup(AgeGroup ageGroup, int responseMs, FsrsRating expected)
    {
        _grader.Grade(ExerciseType.PictureChoice, ageGroup, isCorrect: true, responseMs, hintUsed: false, ageBand: null)
            .Should().Be(expected);
    }

    [Fact]
    public void AnswerGrader_Grade_WrongAnswerIsAgainWhateverTheBand()
    {
        _grader.Grade(ExerciseType.Typing, AgeGroup.Child, isCorrect: false, responseMs: 0, hintUsed: false, AgeBand.Under7)
            .Should().Be(FsrsRating.Again);
    }

    [Fact]
    public void AnswerGrader_Grade_UsesConfiguredBandMultiplier()
    {
        AnswerGrader grader = new(new VocabularyGradingOptions
        {
            AgeBandMultiplier = new AgeBandMultipliers { Under7 = 2.0 },
        });

        grader.Grade(ExerciseType.PictureChoice, AgeGroup.Child, isCorrect: true, responseMs: 6000, hintUsed: false, AgeBand.Under7)
            .Should().Be(FsrsRating.Easy);
    }
}
