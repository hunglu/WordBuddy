using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AcceptInvitation;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminCompleteUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminHandoverPrimary;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminRejectUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.CreateInvitation;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.EscalateUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.RequestUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondToPendingSupporter;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondUnlink;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.UnitTests.Features;

public class SupportLinkHandlerTests
{
    private readonly SupportLinkHandlerFixture _f = new();

    // ── Invitations ─────────────────────────────────────────────────────────

    private CreateInvitationCommandHandler CreateInvitationHandler() =>
        new(_f.Users.Object, _f.Links.Object, _f.Cache.Object, _f.Options, _f.Time, new CreateInvitationCommandValidator(),
            Mock.Of<ILogger<CreateInvitationCommandHandler>>());

    private AcceptInvitationCommandHandler AcceptHandler() =>
        new(_f.Users.Object, _f.Links.Object, _f.Events.Object, _f.Cache.Object, _f.Options, _f.Time,
            new AcceptInvitationCommandValidator(), Mock.Of<ILogger<AcceptInvitationCommandHandler>>());

    private SupportLinkInvitation GivenInvitation(User creator, InvitationSide side, string code = "ABCD2345")
    {
        SupportLinkInvitation invitation = SupportLinkInvitation.Create(
            Guid.NewGuid(), creator.Id, side, SupportRelationship.Parent, code, "token", TestData.Now.AddDays(-1), TimeSpan.FromDays(7));
        _f.Links.Setup(l => l.GetInvitationByHashTrackedAsync(SupportLinkInvitation.HashTypedCode(code), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(invitation));
        _f.Links.Setup(l => l.GetInvitationByHashTrackedAsync(It.Is<string?>(h => h != SupportLinkInvitation.HashTypedCode(code)), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<SupportLinkInvitation>(SupportLinkErrors.InvitationNotFound));
        return invitation;
    }

    [Fact]
    public async Task CreateInvitationCommandHandler_HandleAsync_ChildAsSupporterRejected()
    {
        User child = TestData.Child();
        _f.GivenUsers(child);

        Result<CreatedInvitationDto> result = await CreateInvitationHandler().HandleAsync(
            new CreateInvitationCommand(child.Id, InvitationSide.Supporter, null));

        result.Error.Should().Be(SupportLinkErrors.SupporterMustBeAdult);
    }

    [Fact]
    public async Task CreateInvitationCommandHandler_HandleAsync_ChildAsLearnerReturnsCodeAndToken()
    {
        User child = TestData.Child();
        _f.GivenUsers(child);
        SupportLinkInvitation? staged = null;
        _f.Links.Setup(l => l.AddInvitationAsync(It.IsAny<SupportLinkInvitation>(), It.IsAny<CancellationToken>()))
            .Callback<SupportLinkInvitation, CancellationToken>((i, _) => staged = i)
            .Returns(Task.CompletedTask);

        Result<CreatedInvitationDto> result = await CreateInvitationHandler().HandleAsync(
            new CreateInvitationCommand(child.Id, InvitationSide.Learner, SupportRelationship.Parent));

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().HaveLength(8);
        result.Value.ExpiresAtUtc.Should().Be(TestData.Now.AddDays(7));
        staged!.CodeHash.Should().Be(SupportLinkInvitation.HashTypedCode(result.Value.Code));
    }

    [Fact]
    public async Task AcceptInvitationCommandHandler_HandleAsync_ChildFirstSupporterActivePrimaryAndPublished()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        _f.GivenUsers(child, parent);
        _f.GivenLearnerLinks(child.Id);
        GivenInvitation(child, InvitationSide.Learner);

        Result<SupportLinkDto> result = await AcceptHandler().HandleAsync(new AcceptInvitationCommand(parent.Id, "ABCD2345", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsPrimary.Should().BeTrue();
        result.Value.Status.Should().Be(SupportLinkStatus.Active);
        result.Value.LearnerName.Should().Be("Child learner");
        _f.Activated.Should().ContainSingle();
        _f.Audit.Should().ContainSingle(a => a.Action == SupportLinkAuditAction.LinkActivated);
        _f.Cache.Verify(c => c.RemoveAsync(SupportLinkCache.Key(child.Id), It.IsAny<CancellationToken>()), Times.Once);
        _f.Cache.Verify(c => c.RemoveAsync(SupportLinkCache.Key(parent.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitationCommandHandler_HandleAsync_ExtraChildSupporterPendingNotPublished()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        SupportLink primary = TestData.Link(child, parent);
        _f.GivenUsers(child, parent, teacher);
        _f.GivenLearnerLinks(child.Id, primary);
        GivenInvitation(child, InvitationSide.Learner);

        Result<SupportLinkDto> result = await AcceptHandler().HandleAsync(new AcceptInvitationCommand(teacher.Id, "ABCD2345", null));

        result.Value.Status.Should().Be(SupportLinkStatus.PendingPrimaryApproval);
        _f.Activated.Should().BeEmpty();
        _f.Audit.Should().ContainSingle(a => a.Action == SupportLinkAuditAction.LinkPendingPrimaryApproval);
        _f.Cache.Verify(c => c.RemoveAsync(SupportLinkCache.Key(parent.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptInvitationCommandHandler_HandleAsync_WrongCodeNotFound()
    {
        User adult = TestData.Adult();
        _f.GivenUsers(adult);
        GivenInvitation(TestData.Child(), InvitationSide.Learner);

        Result<SupportLinkDto> result = await AcceptHandler().HandleAsync(new AcceptInvitationCommand(adult.Id, "ZZZZ9999", null));

        result.Error.Should().Be(SupportLinkErrors.InvitationNotFound);
    }

    [Fact]
    public async Task AcceptInvitationCommandHandler_HandleAsync_ExpiredRejected()
    {
        User child = TestData.Child();
        User parent = TestData.Adult();
        _f.GivenUsers(child, parent);
        GivenInvitation(child, InvitationSide.Learner);
        _f.Time.UtcNow = TestData.Now.AddDays(10);

        Result<SupportLinkDto> result = await AcceptHandler().HandleAsync(new AcceptInvitationCommand(parent.Id, "ABCD2345", null));

        result.Error.Should().Be(SupportLinkErrors.InvitationExpired);
        _f.AddedLinks.Should().BeEmpty();
    }

    [Fact]
    public async Task AcceptInvitationCommandHandler_HandleAsync_AlreadyUsedRejected()
    {
        User child = TestData.Child();
        User parent = TestData.Adult();
        _f.GivenUsers(child, parent);
        SupportLinkInvitation invitation = GivenInvitation(child, InvitationSide.Learner);
        invitation.Accept(Guid.NewGuid(), Guid.NewGuid(), TestData.Now);

        Result<SupportLinkDto> result = await AcceptHandler().HandleAsync(new AcceptInvitationCommand(parent.Id, "ABCD2345", null));

        result.Error.Should().Be(SupportLinkErrors.InvitationAlreadyUsed);
    }

    [Fact]
    public async Task AcceptInvitationCommandHandler_HandleAsync_ChildAcceptingAsSupporterRejected()
    {
        User adultLearner = TestData.Adult("Learner");
        User child = TestData.Child();
        _f.GivenUsers(adultLearner, child);
        _f.GivenLearnerLinks(adultLearner.Id);
        GivenInvitation(adultLearner, InvitationSide.Learner);

        Result<SupportLinkDto> result = await AcceptHandler().HandleAsync(new AcceptInvitationCommand(child.Id, "ABCD2345", null));

        result.Error.Should().Be(SupportLinkErrors.SupporterMustBeAdult);
    }

    // ── Primary approval ────────────────────────────────────────────────────

    private RespondToPendingSupporterCommandHandler RespondPendingHandler() =>
        new(_f.Users.Object, _f.Links.Object, _f.Events.Object, _f.Cache.Object, _f.Time,
            new RespondToPendingSupporterCommandValidator(), Mock.Of<ILogger<RespondToPendingSupporterCommandHandler>>());

    [Fact]
    public async Task RespondToPendingSupporterCommandHandler_HandleAsync_PrimaryApprovesAndPublishes()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        SupportLink primary = TestData.Link(child, parent);
        SupportLink pending = TestData.Link(child, teacher, [primary]);
        _f.GivenUsers(child, parent, teacher);
        _f.GivenLearnerLinks(child.Id, primary, pending);
        _f.GivenLink(pending);

        Result result = await RespondPendingHandler().HandleAsync(new RespondToPendingSupporterCommand(parent.Id, pending.Id, Approve: true));

        result.IsSuccess.Should().BeTrue();
        pending.Status.Should().Be(SupportLinkStatus.Active);
        _f.Activated.Should().ContainSingle().Which.Should().Be(pending);
        _f.Audit.Should().ContainSingle(a => a.Action == SupportLinkAuditAction.PrimaryApproved);
    }

    [Fact]
    public async Task RespondToPendingSupporterCommandHandler_HandleAsync_NonPrimaryForbidden()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        SupportLink primary = TestData.Link(child, parent);
        SupportLink pending = TestData.Link(child, teacher, [primary]);
        _f.GivenUsers(child, parent, teacher);
        _f.GivenLearnerLinks(child.Id, primary, pending);
        _f.GivenLink(pending);

        Result result = await RespondPendingHandler().HandleAsync(new RespondToPendingSupporterCommand(teacher.Id, pending.Id, Approve: true));

        result.Error.Should().Be(SupportLinkErrors.Forbidden);
        _f.Activated.Should().BeEmpty();
    }

    // ── Unlink ──────────────────────────────────────────────────────────────

    private RequestUnlinkCommandHandler RequestUnlinkHandler() =>
        new(_f.Links.Object, _f.Resolver, _f.Cache.Object, _f.Time, new RequestUnlinkCommandValidator(),
            Mock.Of<ILogger<RequestUnlinkCommandHandler>>());

    private RespondUnlinkCommandHandler RespondUnlinkHandler() =>
        new(_f.Links.Object, _f.Resolver, _f.Events.Object, _f.GroupCleaner.Object, _f.Cache.Object, _f.Time, new RespondUnlinkCommandValidator(),
            Mock.Of<ILogger<RespondUnlinkCommandHandler>>());

    private EscalateUnlinkCommandHandler EscalateHandler() =>
        new(_f.Links.Object, _f.Resolver, _f.Cache.Object, _f.Options, _f.Time, new EscalateUnlinkCommandValidator(),
            Mock.Of<ILogger<EscalateUnlinkCommandHandler>>());

    [Fact]
    public async Task RequestUnlinkCommandHandler_HandleAsync_ChildCallerForbidden()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        SupportLink primary = TestData.Link(child, parent);
        SupportLink extra = TestData.Link(child, teacher, [primary]);
        extra.ApproveByPrimary(TestData.Now);
        _f.GivenUsers(child, parent, teacher);
        _f.GivenLearnerLinks(child.Id, primary, extra);
        _f.GivenLink(extra);

        Result result = await RequestUnlinkHandler().HandleAsync(new RequestUnlinkCommand(child.Id, extra.Id));

        result.Error.Should().Be(SupportLinkErrors.ChildForbidden);
        _f.AddedRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task RequestUnlinkCommandHandler_HandleAsync_PrimaryLinkRejected()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        SupportLink primary = TestData.Link(child, parent);
        _f.GivenUsers(child, parent);
        _f.GivenLearnerLinks(child.Id, primary);
        _f.GivenLink(primary);

        Result result = await RequestUnlinkHandler().HandleAsync(new RequestUnlinkCommand(parent.Id, primary.Id));

        result.Error.Should().Be(SupportLinkErrors.PrimaryCannotBeUnlinked);
    }

    [Fact]
    public async Task RequestUnlinkCommandHandler_HandleAsync_PrimaryRequestsForChildOnLearnerSide()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        SupportLink primary = TestData.Link(child, parent);
        SupportLink extra = TestData.Link(child, teacher, [primary]);
        extra.ApproveByPrimary(TestData.Now);
        _f.GivenUsers(child, parent, teacher);
        _f.GivenLearnerLinks(child.Id, primary, extra);
        _f.GivenLink(extra);

        Result result = await RequestUnlinkHandler().HandleAsync(new RequestUnlinkCommand(parent.Id, extra.Id));

        result.IsSuccess.Should().BeTrue();
        _f.AddedRequests.Should().ContainSingle().Which.RequestedBySide.Should().Be(LinkSide.Learner);
        _f.Audit.Should().ContainSingle(a => a.Action == SupportLinkAuditAction.UnlinkRequested);
    }

    [Fact]
    public async Task RespondUnlinkCommandHandler_HandleAsync_ConfirmRevokesPublishesAndAudits()
    {
        User learner = TestData.Adult("Learner");
        User partner = TestData.Adult("Partner");
        SupportLink link = TestData.Link(learner, partner);
        _f.GivenUsers(learner, partner);
        _f.GivenLink(link);
        _f.GivenOpenRequest(UnlinkRequest.Create(Guid.NewGuid(), link.Id, learner.Id, LinkSide.Learner, TestData.Now));

        Result result = await RespondUnlinkHandler().HandleAsync(new RespondUnlinkCommand(partner.Id, link.Id, Confirm: true));

        result.IsSuccess.Should().BeTrue();
        link.Status.Should().Be(SupportLinkStatus.Revoked);
        _f.Revoked.Should().ContainSingle().Which.Should().Be(link);
        _f.Audit.Should().ContainSingle(a => a.Action == SupportLinkAuditAction.UnlinkConfirmed && a.ActorId == partner.Id);
    }

    [Fact]
    public async Task RespondUnlinkCommandHandler_HandleAsync_RequesterCannotConfirmOwnRequest()
    {
        User learner = TestData.Adult("Learner");
        User partner = TestData.Adult("Partner");
        SupportLink link = TestData.Link(learner, partner);
        _f.GivenUsers(learner, partner);
        _f.GivenLink(link);
        _f.GivenOpenRequest(UnlinkRequest.Create(Guid.NewGuid(), link.Id, learner.Id, LinkSide.Learner, TestData.Now));

        Result result = await RespondUnlinkHandler().HandleAsync(new RespondUnlinkCommand(learner.Id, link.Id, Confirm: true));

        result.Error.Should().Be(SupportLinkErrors.Forbidden);
        link.IsActive.Should().BeTrue();
        _f.Revoked.Should().BeEmpty();
    }

    [Fact]
    public async Task EscalateUnlinkCommandHandler_HandleAsync_BeforeWaitFailsAfterWaitSucceeds()
    {
        User learner = TestData.Adult("Learner");
        User partner = TestData.Adult("Partner");
        SupportLink link = TestData.Link(learner, partner);
        _f.GivenUsers(learner, partner);
        _f.GivenLink(link);
        UnlinkRequest request = UnlinkRequest.Create(Guid.NewGuid(), link.Id, learner.Id, LinkSide.Learner, TestData.Now);
        _f.GivenOpenRequest(request);

        _f.Time.UtcNow = TestData.Now.AddDays(6);
        (await EscalateHandler().HandleAsync(new EscalateUnlinkCommand(learner.Id, link.Id)))
            .Error.Should().Be(SupportLinkErrors.EscalationTooEarly);

        _f.Time.UtcNow = TestData.Now.AddDays(7);
        (await EscalateHandler().HandleAsync(new EscalateUnlinkCommand(learner.Id, link.Id))).IsSuccess.Should().BeTrue();
        request.Status.Should().Be(UnlinkRequestStatus.OverrideRequested);
    }

    // ── Admin ───────────────────────────────────────────────────────────────

    private UnlinkRequest GivenEscalated(SupportLink link, Guid requesterId)
    {
        UnlinkRequest request = UnlinkRequest.Create(Guid.NewGuid(), link.Id, requesterId, LinkSide.Learner, TestData.Now);
        request.Escalate(requesterId, TestData.Now.AddDays(7), 7);
        _f.Links.Setup(l => l.GetUnlinkRequestTrackedAsync(request.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(request));
        _f.Links.Setup(l => l.GetLinksByIdsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([link]);
        return request;
    }

    [Fact]
    public async Task AdminCompleteUnlinkCommandHandler_HandleAsync_RevokesPublishesAndAuditsReason()
    {
        User learner = TestData.Adult("Learner");
        User partner = TestData.Adult("Partner");
        SupportLink link = TestData.Link(learner, partner);
        _f.GivenLink(link);
        UnlinkRequest request = GivenEscalated(link, learner.Id);
        Guid adminId = Guid.NewGuid();

        AdminCompleteUnlinkCommandHandler handler = new(_f.Links.Object, _f.Events.Object, _f.GroupCleaner.Object, _f.Cache.Object, _f.Time,
            new AdminCompleteUnlinkCommandValidator(), Mock.Of<ILogger<AdminCompleteUnlinkCommandHandler>>());
        Result result = await handler.HandleAsync(new AdminCompleteUnlinkCommand(adminId, request.Id, "No response for 7 days"));

        result.IsSuccess.Should().BeTrue();
        link.Status.Should().Be(SupportLinkStatus.Revoked);
        _f.Revoked.Should().ContainSingle();
        _f.Audit.Should().ContainSingle(a =>
            a.Action == SupportLinkAuditAction.AdminUnlinkCompleted && a.ActorId == adminId && a.Reason == "No response for 7 days");
    }

    [Fact]
    public async Task AdminRejectUnlinkCommandHandler_HandleAsync_ReasonRequired()
    {
        AdminRejectUnlinkCommandHandler handler = new(_f.Links.Object, _f.Cache.Object, _f.Time,
            new AdminRejectUnlinkCommandValidator(), Mock.Of<ILogger<AdminRejectUnlinkCommandHandler>>());

        Result result = await handler.HandleAsync(new AdminRejectUnlinkCommand(Guid.NewGuid(), Guid.NewGuid(), " "));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task AdminRejectUnlinkCommandHandler_HandleAsync_LinkStaysActiveAndAudited()
    {
        User learner = TestData.Adult("Learner");
        SupportLink link = TestData.Link(learner, TestData.Adult("Partner"));
        UnlinkRequest request = GivenEscalated(link, learner.Id);

        AdminRejectUnlinkCommandHandler handler = new(_f.Links.Object, _f.Cache.Object, _f.Time,
            new AdminRejectUnlinkCommandValidator(), Mock.Of<ILogger<AdminRejectUnlinkCommandHandler>>());
        Result result = await handler.HandleAsync(new AdminRejectUnlinkCommand(Guid.NewGuid(), request.Id, "Supporter still needed"));

        result.IsSuccess.Should().BeTrue();
        link.IsActive.Should().BeTrue();
        request.Status.Should().Be(UnlinkRequestStatus.RejectedByAdmin);
        _f.Audit.Should().ContainSingle(a => a.Action == SupportLinkAuditAction.AdminUnlinkRejected);
    }

    [Fact]
    public async Task AdminHandoverPrimaryCommandHandler_HandleAsync_MovesPrimaryAndAuditsBothLinks()
    {
        User child = TestData.Child();
        User parent = TestData.Adult("Parent");
        User teacher = TestData.Adult("Teacher");
        SupportLink primary = TestData.Link(child, parent);
        SupportLink extra = TestData.Link(child, teacher, [primary]);
        extra.ApproveByPrimary(TestData.Now);
        _f.GivenUsers(child, parent, teacher);
        _f.GivenLearnerLinks(child.Id, primary, extra);

        AdminHandoverPrimaryCommandHandler handler = new(_f.Users.Object, _f.Links.Object, _f.Cache.Object, _f.Time,
            new AdminHandoverPrimaryCommandValidator(), Mock.Of<ILogger<AdminHandoverPrimaryCommandHandler>>());
        Result result = await handler.HandleAsync(new AdminHandoverPrimaryCommand(Guid.NewGuid(), child.Id, extra.Id, "Parent asked"));

        result.IsSuccess.Should().BeTrue();
        primary.IsPrimary.Should().BeFalse();
        extra.IsPrimary.Should().BeTrue();
        _f.Audit.Should().HaveCount(2).And.OnlyContain(a => a.Action == SupportLinkAuditAction.AdminPrimaryHandover);
        _f.Links.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AdminHandoverPrimaryCommandHandler_HandleAsync_AdultLearnerRejected()
    {
        User learner = TestData.Adult("Learner");
        _f.GivenUsers(learner);

        AdminHandoverPrimaryCommandHandler handler = new(_f.Users.Object, _f.Links.Object, _f.Cache.Object, _f.Time,
            new AdminHandoverPrimaryCommandValidator(), Mock.Of<ILogger<AdminHandoverPrimaryCommandHandler>>());
        Result result = await handler.HandleAsync(new AdminHandoverPrimaryCommand(Guid.NewGuid(), learner.Id, Guid.NewGuid(), "reason"));

        result.Error.Should().Be(SupportLinkErrors.PrimaryChildOnly);
    }
}
