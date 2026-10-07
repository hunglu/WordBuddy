using System.Reflection;
using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

public class ReviewLogTests
{
    [Fact]
    public void ReviewLog_Create_SetsAllFields()
    {
        Guid id = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid senseId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();
        DateTime at = new(2026, 10, 7, 9, 30, 0, DateTimeKind.Utc);

        ReviewLog log = ReviewLog.Create(
            id, userId, senseId, sessionId, at, ExerciseType.Typing, VocabularySkill.Spelling,
            isCorrect: true, responseMs: 4200, hintUsed: true, isDue: false, attemptNo: 2, FsrsRating.Hard);

        log.Id.Should().Be(id);
        log.UserId.Should().Be(userId);
        log.SenseId.Should().Be(senseId);
        log.SessionId.Should().Be(sessionId);
        log.OccurredAtUtc.Should().Be(at);
        log.ExerciseType.Should().Be(ExerciseType.Typing);
        log.Skill.Should().Be(VocabularySkill.Spelling);
        log.IsCorrect.Should().BeTrue();
        log.ResponseMs.Should().Be(4200);
        log.HintUsed.Should().BeTrue();
        log.IsDue.Should().BeFalse();
        log.AttemptNo.Should().Be(2);
        log.Rating.Should().Be(FsrsRating.Hard);
    }

    [Fact]
    public void ReviewLog_Type_ExposesNoMutators()
    {
        Type type = typeof(ReviewLog);

        type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(p => p.SetMethod is not null)
            .Should().BeEmpty("a review log is insert-only");

        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Should().BeEmpty("no update methods exist");

        type.GetConstructors(BindingFlags.Instance | BindingFlags.Public).Should().BeEmpty();
    }
}
