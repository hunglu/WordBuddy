using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

public class AnswerCheckerTests
{
    [Theory]
    [InlineData(ExerciseType.PictureChoice)]
    [InlineData(ExerciseType.ListeningChoice)]
    public void AnswerChecker_IsCorrect_ChoiceWithRightKey(ExerciseType type)
    {
        AnswerChecker.IsCorrect(type, "k1", "k1", null).Should().BeTrue();
    }

    [Theory]
    [InlineData("k2")]
    [InlineData("K1")]
    [InlineData("")]
    [InlineData(null)]
    public void AnswerChecker_IsCorrect_ChoiceWithWrongOrMissingKey(string? key)
    {
        AnswerChecker.IsCorrect(ExerciseType.PictureChoice, "k1", key, null).Should().BeFalse();
    }

    [Fact]
    public void AnswerChecker_IsCorrect_ChoiceIgnoresTypedText()
    {
        AnswerChecker.IsCorrect(ExerciseType.PictureChoice, "k1", null, "k1").Should().BeFalse();
    }

    [Theory]
    [InlineData("apple")]
    [InlineData("Apple")]
    [InlineData("APPLE")]
    [InlineData("  apple")]
    [InlineData("apple  ")]
    [InlineData("  ApPlE  ")]
    public void AnswerChecker_IsCorrect_TypingTrimsAndIgnoresCase(string typed)
    {
        AnswerChecker.IsCorrect(ExerciseType.Typing, "apple", null, typed).Should().BeTrue();
    }

    [Theory]
    [InlineData("aple")]
    [InlineData("apples")]
    [InlineData("app le")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AnswerChecker_IsCorrect_TypingWrongText(string? typed)
    {
        AnswerChecker.IsCorrect(ExerciseType.Typing, "apple", null, typed).Should().BeFalse();
    }

    [Fact]
    public void AnswerChecker_IsCorrect_TypingIgnoresOptionKey()
    {
        AnswerChecker.IsCorrect(ExerciseType.Typing, "apple", "apple", null).Should().BeFalse();
    }
}
