using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AcceptInvitation;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelInvitation;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.CreateInvitation;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.EscalateUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.RequestUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondToPendingSupporter;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetMySupportLinks;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetPendingSupporterApprovals;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Controllers;

/// <summary>
/// Learner support links: invitations, Primary approval, two-sided unlink and escalation.
/// Child callers may create and accept invitations and read their links; all other link actions
/// return 403 for a child (the Primary supporter acts for the child). Logs ids only.
/// </summary>
[ApiController]
[Route("api/auth/support-links")]
[Authorize]
public sealed class SupportLinksController : ControllerBase
{
    private readonly IQueryHandler<GetMySupportLinksQuery, MySupportLinksDto> _getMine;
    private readonly IQueryHandler<GetPendingSupporterApprovalsQuery, IReadOnlyList<SupportLinkDto>> _getPending;
    private readonly ICommandHandler<CreateInvitationCommand, CreatedInvitationDto> _createInvitation;
    private readonly ICommandHandler<AcceptInvitationCommand, SupportLinkDto> _acceptInvitation;
    private readonly ICommandHandler<CancelInvitationCommand> _cancelInvitation;
    private readonly ICommandHandler<RespondToPendingSupporterCommand> _respondPending;
    private readonly ICommandHandler<RequestUnlinkCommand> _requestUnlink;
    private readonly ICommandHandler<RespondUnlinkCommand> _respondUnlink;
    private readonly ICommandHandler<CancelUnlinkCommand> _cancelUnlink;
    private readonly ICommandHandler<EscalateUnlinkCommand> _escalateUnlink;

    public SupportLinksController(
        IQueryHandler<GetMySupportLinksQuery, MySupportLinksDto> getMine,
        IQueryHandler<GetPendingSupporterApprovalsQuery, IReadOnlyList<SupportLinkDto>> getPending,
        ICommandHandler<CreateInvitationCommand, CreatedInvitationDto> createInvitation,
        ICommandHandler<AcceptInvitationCommand, SupportLinkDto> acceptInvitation,
        ICommandHandler<CancelInvitationCommand> cancelInvitation,
        ICommandHandler<RespondToPendingSupporterCommand> respondPending,
        ICommandHandler<RequestUnlinkCommand> requestUnlink,
        ICommandHandler<RespondUnlinkCommand> respondUnlink,
        ICommandHandler<CancelUnlinkCommand> cancelUnlink,
        ICommandHandler<EscalateUnlinkCommand> escalateUnlink)
    {
        _getMine = getMine;
        _getPending = getPending;
        _createInvitation = createInvitation;
        _acceptInvitation = acceptInvitation;
        _cancelInvitation = cancelInvitation;
        _respondPending = respondPending;
        _requestUnlink = requestUnlink;
        _respondUnlink = respondUnlink;
        _cancelUnlink = cancelUnlink;
        _escalateUnlink = escalateUnlink;
    }

    /// <summary>Returns the caller links (as learner, as supporter, managed as Primary) and open invitations.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        Result<MySupportLinksDto> result = await _getMine.HandleAsync(new GetMySupportLinksQuery(User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Returns extra supporters waiting for the caller (as Primary) to approve.</summary>
    [HttpGet("pending-approvals")]
    public async Task<IActionResult> GetPendingApprovals(CancellationToken ct)
    {
        Result<IReadOnlyList<SupportLinkDto>> result =
            await _getPending.HandleAsync(new GetPendingSupporterApprovalsQuery(User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Creates an invitation code and link token (shown once).</summary>
    [HttpPost("invitations")]
    [EnableRateLimiting(RateLimitingConfiguration.SupportLinkInvitationPolicy)]
    public async Task<IActionResult> CreateInvitation([FromBody] CreateInvitationRequest request, CancellationToken ct)
    {
        Result<CreatedInvitationDto> result = await _createInvitation.HandleAsync(
            new CreateInvitationCommand(User.GetUserId(), request.InviteAs, request.Relationship), ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Cancels an open invitation the caller created.</summary>
    [HttpDelete("invitations/{id:guid}")]
    public async Task<IActionResult> CancelInvitation(Guid id, CancellationToken ct)
    {
        Result result = await _cancelInvitation.HandleAsync(new CancelInvitationCommand(User.GetUserId(), id), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Accepts an invitation by code or token.</summary>
    [HttpPost("accept")]
    [EnableRateLimiting(RateLimitingConfiguration.SupportLinkInvitationPolicy)]
    public async Task<IActionResult> Accept([FromBody] AcceptInvitationRequest request, CancellationToken ct)
    {
        Result<SupportLinkDto> result = await _acceptInvitation.HandleAsync(
            new AcceptInvitationCommand(User.GetUserId(), request.Code, request.Token), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Primary approves a pending extra supporter.</summary>
    [HttpPost("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, CancellationToken ct) => RespondPending(id, approve: true, ct);

    /// <summary>Primary rejects a pending extra supporter.</summary>
    [HttpPost("{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, CancellationToken ct) => RespondPending(id, approve: false, ct);

    /// <summary>Requests to end an active link.</summary>
    [HttpPost("{id:guid}/unlink-request")]
    public async Task<IActionResult> RequestUnlink(Guid id, CancellationToken ct)
    {
        Result result = await _requestUnlink.HandleAsync(new RequestUnlinkCommand(User.GetUserId(), id), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>The other side confirms the unlink; the link is revoked.</summary>
    [HttpPost("{id:guid}/unlink-request/confirm")]
    public Task<IActionResult> ConfirmUnlink(Guid id, CancellationToken ct) => RespondUnlink(id, confirm: true, ct);

    /// <summary>The other side declines the unlink; the link stays active.</summary>
    [HttpPost("{id:guid}/unlink-request/decline")]
    public Task<IActionResult> DeclineUnlink(Guid id, CancellationToken ct) => RespondUnlink(id, confirm: false, ct);

    /// <summary>The requester withdraws the unlink request.</summary>
    [HttpDelete("{id:guid}/unlink-request")]
    public async Task<IActionResult> CancelUnlink(Guid id, CancellationToken ct)
    {
        Result result = await _cancelUnlink.HandleAsync(new CancelUnlinkCommand(User.GetUserId(), id), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>The requester asks an admin to override, after the wait time without any response.</summary>
    [HttpPost("{id:guid}/unlink-request/escalate")]
    public async Task<IActionResult> EscalateUnlink(Guid id, CancellationToken ct)
    {
        Result result = await _escalateUnlink.HandleAsync(new EscalateUnlinkCommand(User.GetUserId(), id), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    private async Task<IActionResult> RespondPending(Guid id, bool approve, CancellationToken ct)
    {
        Result result = await _respondPending.HandleAsync(new RespondToPendingSupporterCommand(User.GetUserId(), id, approve), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    private async Task<IActionResult> RespondUnlink(Guid id, bool confirm, CancellationToken ct)
    {
        Result result = await _respondUnlink.HandleAsync(new RespondUnlinkCommand(User.GetUserId(), id, confirm), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }
}
