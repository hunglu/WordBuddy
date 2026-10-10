using FluentAssertions;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Domain;

public class LearnerGroupPolicyTests
{
    private static LearnerGroup NewGroup(User owner) =>
        LearnerGroup.Create(Guid.NewGuid(), owner.Id, "Class 5A", TestData.Now).Value;

    private static Result<LearnerGroupMember> Add(
        LearnerGroup group,
        User owner,
        User learner,
        bool hasLink = true,
        bool ownerIsPrimary = false,
        bool alreadyMember = false,
        int count = 0,
        int max = 40) =>
        LearnerGroupPolicy.AddMember(
            Guid.NewGuid(), group, TestData.Party(owner), TestData.Party(learner), hasLink, ownerIsPrimary, alreadyMember, count, max, TestData.Now);

    [Fact]
    public void LearnerGroupPolicy_AddMember_NoActiveLinkRejected()
    {
        User owner = TestData.Adult("Teacher");

        Result<LearnerGroupMember> result = Add(NewGroup(owner), owner, TestData.Adult("Learner"), hasLink: false);

        result.Error.Should().Be(LearnerGroupErrors.NoActiveLink);
    }

    [Fact]
    public void LearnerGroupPolicy_AddMember_ChildIsPendingWhenOwnerIsNotPrimary()
    {
        User owner = TestData.Adult("Teacher");

        Result<LearnerGroupMember> result = Add(NewGroup(owner), owner, TestData.Child(), ownerIsPrimary: false);

        result.Value.Status.Should().Be(GroupMemberStatus.PendingPrimaryApproval);
    }

    [Fact]
    public void LearnerGroupPolicy_AddMember_ChildIsActiveWhenOwnerIsPrimary()
    {
        User owner = TestData.Adult("Parent");

        Result<LearnerGroupMember> result = Add(NewGroup(owner), owner, TestData.Child(), ownerIsPrimary: true);

        result.Value.Status.Should().Be(GroupMemberStatus.Active);
    }

    [Fact]
    public void LearnerGroupPolicy_AddMember_AdultIsActiveAtOnce()
    {
        User owner = TestData.Adult("Teacher");

        Result<LearnerGroupMember> result = Add(NewGroup(owner), owner, TestData.Adult("Learner"));

        result.Value.Status.Should().Be(GroupMemberStatus.Active);
    }

    [Fact]
    public void LearnerGroupPolicy_AddMember_DuplicateRejected()
    {
        User owner = TestData.Adult("Teacher");

        Result<LearnerGroupMember> result = Add(NewGroup(owner), owner, TestData.Adult("Learner"), alreadyMember: true);

        result.Error.Should().Be(LearnerGroupErrors.AlreadyMember);
    }

    [Fact]
    public void LearnerGroupPolicy_AddMember_FullGroupRejected()
    {
        User owner = TestData.Adult("Teacher");

        Result<LearnerGroupMember> result = Add(NewGroup(owner), owner, TestData.Adult("Learner"), count: 40, max: 40);

        result.Error.Should().Be(LearnerGroupErrors.GroupFull);
    }

    [Fact]
    public void LearnerGroupPolicy_AddMember_NonOwnerRejected()
    {
        User owner = TestData.Adult("Teacher");
        User other = TestData.Adult("Other");

        Result<LearnerGroupMember> result = Add(NewGroup(owner), other, TestData.Adult("Learner"));

        result.Error.Should().Be(LearnerGroupErrors.NotOwner);
    }

    [Fact]
    public void LearnerGroupPolicy_AddMember_SelfRejected()
    {
        User owner = TestData.Adult("Teacher");

        Result<LearnerGroupMember> result = Add(NewGroup(owner), owner, owner);

        result.Error.Should().Be(LearnerGroupErrors.SelfAdd);
    }

    [Fact]
    public void LearnerGroupPolicy_CanCreateGroup_ChildRejectedAndLimitEnforced()
    {
        LearnerGroupPolicy.CanCreateGroup(TestData.Party(TestData.Child()), 0, 20).Error.Should().Be(LearnerGroupErrors.ChildForbidden);
        LearnerGroupPolicy.CanCreateGroup(TestData.Party(TestData.Adult()), 20, 20).Error.Should().Be(LearnerGroupErrors.TooManyGroups);
        LearnerGroupPolicy.CanCreateGroup(TestData.Party(TestData.Adult()), 19, 20).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void LearnerGroupPolicy_CanRespond_OnlyThePrimaryOfThePendingChild()
    {
        User owner = TestData.Adult("Teacher");
        User child = TestData.Child();
        User primary = TestData.Adult("Parent");
        LearnerGroupMember pending = Add(NewGroup(owner), owner, child).Value;

        LearnerGroupPolicy.CanRespond(pending, TestData.Party(primary), primary.Id).IsSuccess.Should().BeTrue();
        LearnerGroupPolicy.CanRespond(pending, TestData.Party(owner), primary.Id).Error.Should().Be(LearnerGroupErrors.NotPrimary);
        LearnerGroupPolicy.CanRespond(pending, TestData.Party(child), primary.Id).Error.Should().Be(LearnerGroupErrors.ChildForbidden);
    }

    [Fact]
    public void LearnerGroupPolicy_CanLeave_AdultCanChildCannot()
    {
        User owner = TestData.Adult("Teacher");
        User adult = TestData.Adult("Learner");
        User child = TestData.Child();
        LearnerGroup group = NewGroup(owner);
        LearnerGroupMember adultMember = Add(group, owner, adult).Value;
        LearnerGroupMember childMember = Add(group, owner, child, ownerIsPrimary: true).Value;

        LearnerGroupPolicy.CanLeave(adultMember, TestData.Party(adult)).IsSuccess.Should().BeTrue();
        LearnerGroupPolicy.CanLeave(childMember, TestData.Party(child)).Error.Should().Be(LearnerGroupErrors.ChildCannotLeave);
    }
}
