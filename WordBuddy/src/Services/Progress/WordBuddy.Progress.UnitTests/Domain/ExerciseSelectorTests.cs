using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

/// <summary>Every row of the selection table (living spec, vocabulary-builder).</summary>
public class ExerciseSelectorTests
{
    [Theory]
    [InlineData(WordStatus.New, ExerciseLevel.Recognition, true, true, ExerciseType.PictureChoice, VocabularySkill.Meaning)]
    [InlineData(WordStatus.Learning, ExerciseLevel.Recognition, true, false, ExerciseType.PictureChoice, VocabularySkill.Meaning)]
    [InlineData(WordStatus.New, ExerciseLevel.Recognition, false, true, ExerciseType.ListeningChoice, VocabularySkill.Listening)]
    [InlineData(WordStatus.New, ExerciseLevel.Recognition, false, false, ExerciseType.Typing, VocabularySkill.Spelling)]
    [InlineData(WordStatus.Learning, ExerciseLevel.Recall, true, true, ExerciseType.ListeningChoice, VocabularySkill.Listening)]
    [InlineData(WordStatus.Learning, ExerciseLevel.Recall, true, false, ExerciseType.Typing, VocabularySkill.Spelling)]
    [InlineData(WordStatus.Review, ExerciseLevel.Recall, true, true, ExerciseType.Typing, VocabularySkill.Spelling)]
    [InlineData(WordStatus.Mastered, ExerciseLevel.Recall, true, true, ExerciseType.Typing, VocabularySkill.Spelling)]
    [InlineData(WordStatus.Leech, ExerciseLevel.Recognition, true, true, ExerciseType.Typing, VocabularySkill.Spelling)]
    public void ExerciseSelector_Select_FollowsTheSelectionTable(
        WordStatus status, ExerciseLevel level, bool hasImage, bool hasAudio, ExerciseType expectedType, VocabularySkill expectedSkill)
    {
        ExerciseChoice choice = ExerciseSelector.Select(status, level, hasImage, hasAudio, candidateSenseCount: 4);

        choice.ExerciseType.Should().Be(expectedType);
        choice.Skill.Should().Be(expectedSkill);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void ExerciseSelector_Select_FewerThanFourSensesFallsBackToTyping(int senseCount)
    {
        ExerciseChoice choice = ExerciseSelector.Select(WordStatus.New, ExerciseLevel.Recognition, true, true, senseCount);

        choice.ExerciseType.Should().Be(ExerciseType.Typing);
        choice.Skill.Should().Be(VocabularySkill.Spelling);
    }

    [Theory]
    [InlineData(WordStatus.New, 0, ExerciseLevel.Recognition)]
    [InlineData(WordStatus.Learning, 0, ExerciseLevel.Recognition)]
    [InlineData(WordStatus.Learning, 1, ExerciseLevel.Recall)]
    [InlineData(WordStatus.New, 1, ExerciseLevel.Recall)]
    [InlineData(WordStatus.Review, 0, ExerciseLevel.Recall)]
    [InlineData(WordStatus.Mastered, 0, ExerciseLevel.Recall)]
    [InlineData(WordStatus.Leech, 0, ExerciseLevel.Recall)]
    public void ExerciseSelector_LevelFor_RecallAfterACorrectAnswerOrForRecallOnlyStatuses(
        WordStatus status, int correctAnswers, ExerciseLevel expected)
    {
        ExerciseSelector.LevelFor(status, correctAnswers).Should().Be(expected);
    }
}
