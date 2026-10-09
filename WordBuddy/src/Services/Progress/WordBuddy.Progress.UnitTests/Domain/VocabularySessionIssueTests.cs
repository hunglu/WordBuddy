using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

public class VocabularySessionIssueTests
{
    [Fact]
    public void VocabularySessionIssue_Create_SetsAllFields()
    {
        Guid sessionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        DateTime issuedAt = new(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);

        VocabularySessionIssue issue = VocabularySessionIssue.Create(sessionId, userId, issuedAt, 12);

        issue.SessionId.Should().Be(sessionId);
        issue.UserId.Should().Be(userId);
        issue.IssuedAtUtc.Should().Be(issuedAt);
        issue.PlannedCount.Should().Be(12);
    }

    [Fact]
    public void VocabularySessionIssue_Shape_HasNoSettersOrMutators()
    {
        typeof(VocabularySessionIssue).GetProperties().Should().OnlyContain(p => p.SetMethod == null);
        typeof(VocabularySessionIssue).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Should().BeEmpty();
    }
}
