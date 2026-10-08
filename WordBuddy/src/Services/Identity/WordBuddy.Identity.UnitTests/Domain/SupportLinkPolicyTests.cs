using FluentAssertions;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Domain;

public class SupportLinkPolicyTests
{
    [Fact]
    public void SupportLinkPolicy_CreateOnAccept_ChildFirstLinkIsActivePrimary()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");

        SupportLink link = TestData.Link(child, parent);

        link.Status.Should().Be(SupportLinkStatus.Active);
        link.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void SupportLinkPolicy_CreateOnAccept_ExtraChildLinkIsPendingPrimaryApproval()
    {
        User child = TestData.Child();
        SupportLink primary = TestData.Link(child, TestData.Adult("Parent"));

        SupportLink extra = TestData.Link(child, TestData.Adult("Teacher"), [primary]);

        extra.Status.Should().Be(SupportLinkStatus.PendingPrimaryApproval);
        extra.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void SupportLinkPolicy_CreateOnAccept_ChildSupporterRejected()
    {
        Result<SupportLink> result = SupportLinkPolicy.CreateOnAccept(
            Guid.NewGuid(), TestData.Party(TestData.Adult()), TestData.Party(TestData.Child()), null, [], Guid.NewGuid(), TestData.Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SupportLinkErrors.SupporterMustBeAdult);
    }

    [Fact]
    public void SupportLinkPolicy_CreateOnAccept_AdultLearnerAcceptedAloneWithoutPrimary()
    {
        User adultLearner = TestData.Adult("Learner");
        SupportLink first = TestData.Link(adultLearner, TestData.Adult("Partner"));

        SupportLink second = TestData.Link(adultLearner, TestData.Adult("Teacher"), [first]);

        first.Status.Should().Be(SupportLinkStatus.Active);
        first.IsPrimary.Should().BeFalse();
        second.Status.Should().Be(SupportLinkStatus.Active);
        second.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void SupportLinkPolicy_CreateOnAccept_SamePairTwiceIsConflict()
    {
        User learner = TestData.Adult("Learner");
        User supporter = TestData.Adult("Partner");
        SupportLink first = TestData.Link(learner, supporter);

        Result<SupportLink> result = SupportLinkPolicy.CreateOnAccept(
            Guid.NewGuid(), TestData.Party(learner), TestData.Party(supporter), null, [first], supporter.Id, TestData.Now);

        result.Error.Should().Be(SupportLinkErrors.AlreadyLinked);
    }

    [Fact]
    public void SupportLinkPolicy_CreateOnAccept_SelfLinkRejected()
    {
        User adult = TestData.Adult();

        Result<SupportLink> result = SupportLinkPolicy.CreateOnAccept(
            Guid.NewGuid(), TestData.Party(adult), TestData.Party(adult), null, [], adult.Id, TestData.Now);

        result.Error.Should().Be(SupportLinkErrors.SelfLink);
    }

    [Fact]
    public void SupportLinkPolicy_CanRequestUnlink_PrimaryRejected()
    {
        SupportLink primary = TestData.Link(TestData.Child(), TestData.Adult());

        SupportLinkPolicy.CanRequestUnlink(primary).Error.Should().Be(SupportLinkErrors.PrimaryCannotBeUnlinked);
        primary.Revoke(TestData.Now).Error.Should().Be(SupportLinkErrors.PrimaryCannotBeUnlinked);
    }

    [Fact]
    public void SupportLinkPolicy_ResolveActorSide_ChildActorForbidden()
    {
        User child = TestData.Child();
        SupportLink link = TestData.Link(child, TestData.Adult());

        Result<LinkSide> side = SupportLinkPolicy.ResolveActorSide(link, TestData.Party(child), learnerIsChild: true, link.SupporterId);

        side.Error.Should().Be(SupportLinkErrors.ChildForbidden);
    }

    [Fact]
    public void SupportLinkPolicy_ResolveActorSide_PrimaryActsForChildLearner()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        SupportLink primary = TestData.Link(child, parent);
        SupportLink extra = TestData.Link(child, teacher, [primary]);
        extra.ApproveByPrimary(TestData.Now);

        Result<LinkSide> side = SupportLinkPolicy.ResolveActorSide(extra, TestData.Party(parent), learnerIsChild: true, parent.Id);

        side.Value.Should().Be(LinkSide.Learner);
    }

    [Fact]
    public void SupportLinkPolicy_ResolveActorSide_StrangerForbidden()
    {
        SupportLink link = TestData.Link(TestData.Adult("Learner"), TestData.Adult("Partner"));

        Result<LinkSide> side = SupportLinkPolicy.ResolveActorSide(link, TestData.Party(TestData.Adult("Other")), false, null);

        side.Error.Should().Be(SupportLinkErrors.Forbidden);
    }

    [Fact]
    public void SupportLinkPolicy_CanRespondToPending_OnlyPrimaryAllowed()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        SupportLink primary = TestData.Link(child, parent);
        SupportLink pending = TestData.Link(child, teacher, [primary]);

        SupportLinkPolicy.CanRespondToPending(pending, TestData.Party(parent), parent.Id).IsSuccess.Should().BeTrue();
        SupportLinkPolicy.CanRespondToPending(pending, TestData.Party(teacher), parent.Id).Error.Should().Be(SupportLinkErrors.Forbidden);
        SupportLinkPolicy.CanRespondToPending(pending, TestData.Party(child), parent.Id).Error.Should().Be(SupportLinkErrors.ChildForbidden);
    }

    [Fact]
    public void SupportLink_MakePrimary_OnlyActiveLink()
    {
        User child = TestData.Child();
        SupportLink primary = TestData.Link(child, TestData.Adult("Parent"));
        SupportLink pending = TestData.Link(child, TestData.Adult("Teacher"), [primary]);

        pending.MakePrimary(TestData.Now).Error.Should().Be(SupportLinkErrors.InvalidStatus);

        pending.ApproveByPrimary(TestData.Now);
        primary.ClearPrimary(TestData.Now);
        pending.MakePrimary(TestData.Now).IsSuccess.Should().BeTrue();
        pending.IsPrimary.Should().BeTrue();
        primary.Revoke(TestData.Now).IsSuccess.Should().BeTrue();
    }
}
