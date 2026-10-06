using FluentAssertions;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Domain;

public class LearnerWordMembershipTests
{
    private static readonly DateTime T0 = new(2026, 10, 5, 10, 0, 0, DateTimeKind.Utc);

    private static LearnerWordMembership CreateActive(DateTime addedAtUtc) =>
        LearnerWordMembership.CreateAdded(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), addedAtUtc);

    [Fact]
    public void LearnerWordMembership_CreateAdded_IsActiveWithEventTime()
    {
        LearnerWordMembership membership = CreateActive(T0);

        membership.IsActive.Should().BeTrue();
        membership.AddedAtUtc.Should().Be(T0);
        membership.LastEventAtUtc.Should().Be(T0);
    }

    [Fact]
    public void LearnerWordMembership_RecordRemoved_NewerEventDeactivatesAndKeepsRow()
    {
        LearnerWordMembership membership = CreateActive(T0);

        Result<bool> result = membership.RecordRemoved(T0.AddMinutes(1));

        result.Value.Should().BeTrue();
        membership.IsActive.Should().BeFalse();
        membership.LastEventAtUtc.Should().Be(T0.AddMinutes(1));
    }

    [Fact]
    public void LearnerWordMembership_RecordRemoved_OlderEventIgnored()
    {
        LearnerWordMembership membership = CreateActive(T0);

        Result<bool> result = membership.RecordRemoved(T0.AddMinutes(-1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        membership.IsActive.Should().BeTrue();
    }

    [Fact]
    public void LearnerWordMembership_RecordAdded_OlderEventAfterRemoveIgnored()
    {
        LearnerWordMembership membership = CreateActive(T0);
        membership.RecordRemoved(T0.AddMinutes(2));

        Result<bool> result = membership.RecordAdded(Guid.NewGuid(), T0.AddMinutes(1));

        result.Value.Should().BeFalse();
        membership.IsActive.Should().BeFalse();
    }

    [Fact]
    public void LearnerWordMembership_RecordAdded_ReAddAfterRemoveWorks()
    {
        LearnerWordMembership membership = CreateActive(T0);
        membership.RecordRemoved(T0.AddMinutes(1));
        Guid addedBy = Guid.NewGuid();

        Result<bool> result = membership.RecordAdded(addedBy, T0.AddMinutes(2));

        result.Value.Should().BeTrue();
        membership.IsActive.Should().BeTrue();
        membership.AddedBy.Should().Be(addedBy);
        membership.AddedAtUtc.Should().Be(T0.AddMinutes(2));
    }

    [Fact]
    public void LearnerWordMembership_CreateRemoved_OlderAddArrivingLaterIgnored()
    {
        LearnerWordMembership membership = LearnerWordMembership.CreateRemoved(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), T0);

        Result<bool> result = membership.RecordAdded(Guid.NewGuid(), T0.AddMinutes(-1));

        result.Value.Should().BeFalse();
        membership.IsActive.Should().BeFalse();
    }
}
