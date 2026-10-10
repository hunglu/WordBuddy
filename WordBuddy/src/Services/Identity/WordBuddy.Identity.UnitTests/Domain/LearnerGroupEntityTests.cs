using FluentAssertions;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Domain;

public class LearnerGroupEntityTests
{
    private static LearnerGroupMember Pending()
    {
        User owner = TestData.Adult("Teacher");
        LearnerGroup group = LearnerGroup.Create(Guid.NewGuid(), owner.Id, "Class 5A", TestData.Now).Value;
        return LearnerGroupPolicy.AddMember(
            Guid.NewGuid(), group, TestData.Party(owner), TestData.Party(TestData.Child()), true, false, false, 0, 40, TestData.Now).Value;
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("")]
    [InlineData("   ")]
    public void LearnerGroup_Create_ShortNameRejected(string name)
    {
        Result<LearnerGroup> result = LearnerGroup.Create(Guid.NewGuid(), Guid.NewGuid(), name, TestData.Now);

        result.Error.Should().Be(LearnerGroupErrors.NameLength);
    }

    [Fact]
    public void LearnerGroup_Create_TooLongNameRejectedAndNameTrimmed()
    {
        LearnerGroup.Create(Guid.NewGuid(), Guid.NewGuid(), new string('a', 61), TestData.Now).Error.Should().Be(LearnerGroupErrors.NameLength);
        LearnerGroup.Create(Guid.NewGuid(), Guid.NewGuid(), "  Class 5A  ", TestData.Now).Value.Name.Should().Be("Class 5A");
    }

    [Fact]
    public void LearnerGroup_Delete_SecondDeleteAndRenameAfterDeleteFail()
    {
        LearnerGroup group = LearnerGroup.Create(Guid.NewGuid(), Guid.NewGuid(), "Class 5A", TestData.Now).Value;

        group.Delete(TestData.Now).IsSuccess.Should().BeTrue();

        group.IsDeleted.Should().BeTrue();
        group.Delete(TestData.Now).IsFailure.Should().BeTrue();
        group.Rename("Class 5B", TestData.Now).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void LearnerGroupMember_Approve_PendingBecomesActive()
    {
        LearnerGroupMember member = Pending();

        member.Approve(TestData.Now).IsSuccess.Should().BeTrue();

        member.Status.Should().Be(GroupMemberStatus.Active);
    }

    [Fact]
    public void LearnerGroupMember_Reject_PendingBecomesRemovedWithReason()
    {
        LearnerGroupMember member = Pending();

        member.Reject(TestData.Now).IsSuccess.Should().BeTrue();

        member.Status.Should().Be(GroupMemberStatus.Removed);
        member.RemovedReason.Should().Be(GroupMemberRemovedReason.RejectedByPrimary);
    }

    [Fact]
    public void LearnerGroupMember_ApproveOrRejectWhenActive_Fails()
    {
        LearnerGroupMember member = Pending();
        member.Approve(TestData.Now);

        member.Approve(TestData.Now).Error.Should().Be(LearnerGroupErrors.InvalidStatus);
        member.Reject(TestData.Now).Error.Should().Be(LearnerGroupErrors.InvalidStatus);
    }

    [Fact]
    public void LearnerGroupMember_Remove_ActiveRemovedOnceThenFinal()
    {
        LearnerGroupMember member = Pending();
        member.Approve(TestData.Now);

        member.Remove(GroupMemberRemovedReason.ByOwner, TestData.Now).IsSuccess.Should().BeTrue();

        member.RemovedReason.Should().Be(GroupMemberRemovedReason.ByOwner);
        member.Remove(GroupMemberRemovedReason.Left, TestData.Now).Error.Should().Be(LearnerGroupErrors.InvalidStatus);
        member.Approve(TestData.Now).Error.Should().Be(LearnerGroupErrors.InvalidStatus);
    }

    [Fact]
    public void LearnerGroupMember_Remove_PendingCanBeRemoved()
    {
        LearnerGroupMember member = Pending();

        member.Remove(GroupMemberRemovedReason.GroupDeleted, TestData.Now).IsSuccess.Should().BeTrue();

        member.Status.Should().Be(GroupMemberStatus.Removed);
    }
}
