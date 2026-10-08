using FluentAssertions;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.UnitTests.Domain;

public class UnlinkRequestTests
{
    private const int WaitDays = 7;

    private readonly Guid _requester = Guid.NewGuid();
    private readonly Guid _otherSide = Guid.NewGuid();
    private readonly Guid _admin = Guid.NewGuid();

    private UnlinkRequest Create() => UnlinkRequest.Create(Guid.NewGuid(), Guid.NewGuid(), _requester, LinkSide.Learner, TestData.Now);

    [Fact]
    public void UnlinkRequest_Escalate_BeforeWaitFails()
    {
        UnlinkRequest request = Create();

        request.Escalate(_requester, TestData.Now.AddDays(WaitDays).AddMinutes(-1), WaitDays)
            .Error.Should().Be(SupportLinkErrors.EscalationTooEarly);
        request.Status.Should().Be(UnlinkRequestStatus.Pending);
    }

    [Fact]
    public void UnlinkRequest_Escalate_AfterWaitSucceeds()
    {
        UnlinkRequest request = Create();

        request.Escalate(_requester, TestData.Now.AddDays(WaitDays), WaitDays).IsSuccess.Should().BeTrue();

        request.Status.Should().Be(UnlinkRequestStatus.OverrideRequested);
        request.EscalatedAtUtc.Should().Be(TestData.Now.AddDays(WaitDays));
    }

    [Fact]
    public void UnlinkRequest_Escalate_AfterDeclineFails()
    {
        UnlinkRequest request = Create();
        request.Decline(_otherSide, LinkSide.Supporter, TestData.Now.AddDays(1)).IsSuccess.Should().BeTrue();

        request.Escalate(_requester, TestData.Now.AddDays(30), WaitDays).Error.Should().Be(SupportLinkErrors.UnlinkInvalidStatus);
    }

    [Fact]
    public void UnlinkRequest_Escalate_NonRequesterForbidden()
    {
        UnlinkRequest request = Create();

        request.Escalate(_otherSide, TestData.Now.AddDays(30), WaitDays).Error.Should().Be(SupportLinkErrors.Forbidden);
    }

    [Fact]
    public void UnlinkRequest_Confirm_SameSideForbidden()
    {
        UnlinkRequest request = Create();

        request.Confirm(_requester, LinkSide.Learner, TestData.Now).Error.Should().Be(SupportLinkErrors.Forbidden);
        request.Confirm(_otherSide, LinkSide.Learner, TestData.Now).Error.Should().Be(SupportLinkErrors.Forbidden);
        request.Confirm(_otherSide, LinkSide.Supporter, TestData.Now).IsSuccess.Should().BeTrue();
        request.Status.Should().Be(UnlinkRequestStatus.Confirmed);
    }

    [Fact]
    public void UnlinkRequest_Cancel_OnlyRequester()
    {
        UnlinkRequest request = Create();

        request.Cancel(_otherSide, TestData.Now).Error.Should().Be(SupportLinkErrors.Forbidden);
        request.Cancel(_requester, TestData.Now).IsSuccess.Should().BeTrue();
        request.IsOpen.Should().BeFalse();
    }

    [Fact]
    public void UnlinkRequest_CompleteByAdmin_OnlyAfterEscalation()
    {
        UnlinkRequest request = Create();
        request.CompleteByAdmin(_admin, TestData.Now).Error.Should().Be(SupportLinkErrors.UnlinkInvalidStatus);

        request.Escalate(_requester, TestData.Now.AddDays(WaitDays), WaitDays);

        request.CompleteByAdmin(_admin, TestData.Now.AddDays(8)).IsSuccess.Should().BeTrue();
        request.Status.Should().Be(UnlinkRequestStatus.CompletedByAdmin);
        request.ResolvedById.Should().Be(_admin);
    }

    [Fact]
    public void UnlinkRequest_RejectByAdmin_ClosesRequest()
    {
        UnlinkRequest request = Create();
        request.Escalate(_requester, TestData.Now.AddDays(WaitDays), WaitDays);

        request.RejectByAdmin(_admin, TestData.Now.AddDays(8)).IsSuccess.Should().BeTrue();

        request.Status.Should().Be(UnlinkRequestStatus.RejectedByAdmin);
        request.IsOpen.Should().BeFalse();
    }
}
