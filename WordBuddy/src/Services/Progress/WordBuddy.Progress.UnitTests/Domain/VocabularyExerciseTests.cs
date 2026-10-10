using FluentAssertions;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Domain;

public class VocabularyExerciseTests
{
    private static readonly DateTime Issued = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

    private static VocabularyExercise NewExercise() => VocabularyExercise.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ExerciseType.Typing, VocabularySkill.Spelling,
        "apple", "Apple", new Dictionary<string, Guid>(), Issued);

    [Fact]
    public void VocabularyExercise_Create_StartsOpen()
    {
        VocabularyExercise exercise = NewExercise();

        exercise.AnsweredAtUtc.Should().BeNull();
        exercise.ExerciseId.Should().Be(exercise.Id);
        exercise.IssuedAtUtc.Should().Be(Issued);
    }

    [Fact]
    public void VocabularyExercise_Answer_FirstCallSetsAnsweredAt()
    {
        VocabularyExercise exercise = NewExercise();

        Result result = exercise.Answer(Issued.AddSeconds(4));

        result.IsSuccess.Should().BeTrue();
        exercise.AnsweredAtUtc.Should().Be(Issued.AddSeconds(4));
    }

    [Fact]
    public void VocabularyExercise_Answer_SecondCallFailsAndKeepsFirstTime()
    {
        VocabularyExercise exercise = NewExercise();
        exercise.Answer(Issued.AddSeconds(4));

        Result second = exercise.Answer(Issued.AddSeconds(9));

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("Exercise.AlreadyAnswered");
        second.Error.Type.Should().Be(ErrorType.Conflict);
        exercise.AnsweredAtUtc.Should().Be(Issued.AddSeconds(4));
    }
}
