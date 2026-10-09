using FluentAssertions;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.UnitTests.Domain;

public class VocabularySessionIssueTests
{
    private static readonly DateTime Issued = new(2026, 10, 9, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime DayEnd = new(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc);

    private static VocabularySessionIssue CreateIssue(DateTime issuedAt, DateTime dayEnd, int duration = 30) =>
        VocabularySessionIssue.Create(
            Guid.NewGuid(), Guid.NewGuid(), issuedAt, [(Guid.NewGuid(), false), (Guid.NewGuid(), true)], duration, dayEnd);

    [Fact]
    public void VocabularySessionIssue_Create_SetsAllFields()
    {
        Guid sessionId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        Guid due = Guid.NewGuid();
        Guid fresh = Guid.NewGuid();

        VocabularySessionIssue issue = VocabularySessionIssue.Create(sessionId, userId, Issued, [(due, false), (fresh, true)], 30, DayEnd);

        issue.SessionId.Should().Be(sessionId);
        issue.UserId.Should().Be(userId);
        issue.IssuedAtUtc.Should().Be(Issued);
        issue.PlannedCount.Should().Be(2);
        issue.DurationMinutes.Should().Be(30);
        issue.ExpiresAtUtc.Should().Be(Issued.AddMinutes(30));
        issue.EndedAtUtc.Should().BeNull();
        issue.Items.Select(i => (i.SenseId, i.IsNew, i.Position)).Should().Equal((due, false, 0), (fresh, true, 1));
    }

    [Fact]
    public void VocabularySessionIssue_Create_ExpiryIsCappedAtLocalDayEnd()
    {
        DateTime lateIssue = DayEnd.AddMinutes(-10);

        VocabularySessionIssue issue = CreateIssue(lateIssue, DayEnd);

        issue.ExpiresAtUtc.Should().Be(DayEnd);
    }

    [Fact]
    public void VocabularySessionIssue_IsOpen_TrueBeforeExpiry()
    {
        VocabularySessionIssue issue = CreateIssue(Issued, DayEnd);

        issue.IsOpen(Issued.AddMinutes(29)).Should().BeTrue();
    }

    [Fact]
    public void VocabularySessionIssue_IsOpen_FalseAtAndAfterExpiry()
    {
        VocabularySessionIssue issue = CreateIssue(Issued, DayEnd);

        issue.IsOpen(Issued.AddMinutes(30)).Should().BeFalse();
        issue.IsOpen(Issued.AddMinutes(31)).Should().BeFalse();
    }

    [Fact]
    public void VocabularySessionIssue_End_ClosesSessionBeforeExpiry()
    {
        VocabularySessionIssue issue = CreateIssue(Issued, DayEnd);
        DateTime endedAt = Issued.AddMinutes(5);

        issue.End(endedAt);

        issue.EndedAtUtc.Should().Be(endedAt);
        issue.IsOpen(Issued.AddMinutes(6)).Should().BeFalse();
    }

    [Fact]
    public void VocabularySessionIssue_Shape_OnlyEndedAtUtcHasASetter()
    {
        typeof(VocabularySessionIssue).GetProperties()
            .Where(p => p.SetMethod != null)
            .Select(p => p.Name)
            .Should().Equal(nameof(VocabularySessionIssue.EndedAtUtc));
        typeof(VocabularySessionIssueItem).GetProperties().Should().OnlyContain(p => p.SetMethod == null);
    }
}
