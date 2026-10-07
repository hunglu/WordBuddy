using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

public class AnswerGraderTests
{
    private readonly AnswerGrader _grader = new(new VocabularyGradingOptions());

    [Theory]
    [InlineData(ExerciseType.PictureChoice)]
    [InlineData(ExerciseType.ListeningChoice)]
    [InlineData(ExerciseType.Typing)]
    public void AnswerGrader_Grade_WrongIsAgainEvenWhenFast(ExerciseType exerciseType)
    {
        _grader.Grade(exerciseType, AgeGroup.Adult, isCorrect: false, responseMs: 0, hintUsed: false)
            .Should().Be(FsrsRating.Again);
    }

    [Theory]
    [InlineData(ExerciseType.PictureChoice)]
    [InlineData(ExerciseType.ListeningChoice)]
    [InlineData(ExerciseType.Typing)]
    public void AnswerGrader_Grade_HintIsHardEvenWhenFast(ExerciseType exerciseType)
    {
        _grader.Grade(exerciseType, AgeGroup.Adult, isCorrect: true, responseMs: 0, hintUsed: true)
            .Should().Be(FsrsRating.Hard);
    }

    [Theory]
    // Adult (×1.0): fast boundary is Easy, one ms more is Good; slow boundary is Hard, one ms less is Good.
    [InlineData(ExerciseType.PictureChoice, 3000, FsrsRating.Easy)]
    [InlineData(ExerciseType.PictureChoice, 3001, FsrsRating.Good)]
    [InlineData(ExerciseType.PictureChoice, 9999, FsrsRating.Good)]
    [InlineData(ExerciseType.PictureChoice, 10000, FsrsRating.Hard)]
    [InlineData(ExerciseType.ListeningChoice, 4000, FsrsRating.Easy)]
    [InlineData(ExerciseType.ListeningChoice, 4001, FsrsRating.Good)]
    [InlineData(ExerciseType.ListeningChoice, 11999, FsrsRating.Good)]
    [InlineData(ExerciseType.ListeningChoice, 12000, FsrsRating.Hard)]
    [InlineData(ExerciseType.Typing, 6000, FsrsRating.Easy)]
    [InlineData(ExerciseType.Typing, 6001, FsrsRating.Good)]
    [InlineData(ExerciseType.Typing, 19999, FsrsRating.Good)]
    [InlineData(ExerciseType.Typing, 20000, FsrsRating.Hard)]
    public void AnswerGrader_Grade_AdultBoundaries(ExerciseType exerciseType, int responseMs, FsrsRating expected)
    {
        _grader.Grade(exerciseType, AgeGroup.Adult, isCorrect: true, responseMs, hintUsed: false)
            .Should().Be(expected);
    }

    [Theory]
    // Child (×1.25): PictureChoice 3750 / 12500, ListeningChoice 5000 / 15000, Typing 7500 / 25000.
    [InlineData(ExerciseType.PictureChoice, 3750, FsrsRating.Easy)]
    [InlineData(ExerciseType.PictureChoice, 3751, FsrsRating.Good)]
    [InlineData(ExerciseType.PictureChoice, 10000, FsrsRating.Good)]
    [InlineData(ExerciseType.PictureChoice, 12499, FsrsRating.Good)]
    [InlineData(ExerciseType.PictureChoice, 12500, FsrsRating.Hard)]
    [InlineData(ExerciseType.ListeningChoice, 5000, FsrsRating.Easy)]
    [InlineData(ExerciseType.ListeningChoice, 15000, FsrsRating.Hard)]
    [InlineData(ExerciseType.Typing, 7500, FsrsRating.Easy)]
    [InlineData(ExerciseType.Typing, 24999, FsrsRating.Good)]
    [InlineData(ExerciseType.Typing, 25000, FsrsRating.Hard)]
    public void AnswerGrader_Grade_ChildBoundariesAreScaled(ExerciseType exerciseType, int responseMs, FsrsRating expected)
    {
        _grader.Grade(exerciseType, AgeGroup.Child, isCorrect: true, responseMs, hintUsed: false)
            .Should().Be(expected);
    }

    [Fact]
    public void AnswerGrader_Grade_UsesConfiguredMultiplier()
    {
        AnswerGrader grader = new(new VocabularyGradingOptions
        {
            AgeGroupMultiplier = new AgeGroupMultipliers { Child = 2.0, Adult = 1.0 },
        });

        grader.Grade(ExerciseType.PictureChoice, AgeGroup.Child, isCorrect: true, responseMs: 6000, hintUsed: false)
            .Should().Be(FsrsRating.Easy);
    }
}
