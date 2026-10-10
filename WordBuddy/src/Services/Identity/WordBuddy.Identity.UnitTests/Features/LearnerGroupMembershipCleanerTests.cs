using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Identity.Application.Features.Groups;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;

namespace WordBuddy.Identity.UnitTests.Features;

public class LearnerGroupMembershipCleanerTests
{
    private readonly GroupHandlerFixture _f = new();

    private LearnerGroupMembershipCleaner Cleaner() =>
        new(_f.Groups.Object, _f.Events.Object, Mock.Of<ILogger<LearnerGroupMembershipCleaner>>());

    [Fact]
    public async Task LearnerGroupMembershipCleaner_RemoveForLinkAsync_RemovesAdultAndChildMembersAndPublishesForActive()
    {
        User teacher = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        User child = TestData.Child();
        LearnerGroup groupA = _f.GivenGroup(teacher, "Group A");
        LearnerGroup groupB = _f.GivenGroup(teacher, "Group B");
        LearnerGroupMember adultMember = _f.GivenMember(groupA, teacher, adult);
        LearnerGroupMember childActive = _f.GivenMember(groupA, teacher, child, ownerIsPrimary: true);
        LearnerGroupMember childPending = _f.GivenMember(groupB, teacher, child, ownerIsPrimary: false);

        IReadOnlyList<Guid> adultGroups = await Cleaner().RemoveForLinkAsync(adult.Id, teacher.Id, TestData.Now);
        IReadOnlyList<Guid> childGroups = await Cleaner().RemoveForLinkAsync(child.Id, teacher.Id, TestData.Now);

        adultGroups.Should().BeEquivalentTo([groupA.Id]);
        childGroups.Should().BeEquivalentTo([groupA.Id, groupB.Id]);
        adultMember.RemovedReason.Should().Be(GroupMemberRemovedReason.LinkRevoked);
        childActive.RemovedReason.Should().Be(GroupMemberRemovedReason.LinkRevoked);
        childPending.RemovedReason.Should().Be(GroupMemberRemovedReason.LinkRevoked);
        _f.Removed.Should().HaveCount(2);
        _f.Removed.Should().OnlyContain(r => r.Reason == GroupMemberRemovedReason.LinkRevoked);
        _f.Removed.Select(r => r.LearnerId).Should().BeEquivalentTo([adult.Id, child.Id]);
    }

    [Fact]
    public async Task LearnerGroupMembershipCleaner_RemoveForLinkAsync_OtherOwnersGroupsUntouched()
    {
        User teacher = TestData.Adult("Teacher");
        User otherTeacher = TestData.Adult("OtherTeacher");
        User adult = TestData.Adult("Learner");
        LearnerGroup mine = _f.GivenGroup(teacher, "Mine");
        LearnerGroup theirs = _f.GivenGroup(otherTeacher, "Theirs");
        LearnerGroupMember inMine = _f.GivenMember(mine, teacher, adult);
        LearnerGroupMember inTheirs = _f.GivenMember(theirs, otherTeacher, adult);

        IReadOnlyList<Guid> affected = await Cleaner().RemoveForLinkAsync(adult.Id, teacher.Id, TestData.Now);

        affected.Should().BeEquivalentTo([mine.Id]);
        inMine.Status.Should().Be(GroupMemberStatus.Removed);
        inTheirs.IsActive.Should().BeTrue();
        _f.Removed.Should().ContainSingle(r => r.GroupId == mine.Id);
    }
}
