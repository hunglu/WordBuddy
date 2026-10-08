using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminCompleteUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminHandoverPrimary;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminRejectUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetLearnerSupportLinksForAdmin;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Controllers;

/// <summary>Admin override of escalated unlinks and Primary handover. Every action is audited with a reason.</summary>
[ApiController]
[Route("api/auth/admin")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminSupportLinksController : ControllerBase
{
    private readonly IQueryHandler<GetUnlinkRequestsForAdminQuery, IReadOnlyList<AdminUnlinkRequestDto>> _getUnlinkRequests;
    private readonly IQueryHandler<GetLearnerSupportLinksForAdminQuery, IReadOnlyList<AdminSupportLinkDto>> _getLearnerLinks;
    private readonly ICommandHandler<AdminCompleteUnlinkCommand> _complete;
    private readonly ICommandHandler<AdminRejectUnlinkCommand> _reject;
    private readonly ICommandHandler<AdminHandoverPrimaryCommand> _handover;

    public AdminSupportLinksController(
        IQueryHandler<GetUnlinkRequestsForAdminQuery, IReadOnlyList<AdminUnlinkRequestDto>> getUnlinkRequests,
        IQueryHandler<GetLearnerSupportLinksForAdminQuery, IReadOnlyList<AdminSupportLinkDto>> getLearnerLinks,
        ICommandHandler<AdminCompleteUnlinkCommand> complete,
        ICommandHandler<AdminRejectUnlinkCommand> reject,
        ICommandHandler<AdminHandoverPrimaryCommand> handover)
    {
        _getUnlinkRequests = getUnlinkRequests;
        _getLearnerLinks = getLearnerLinks;
        _complete = complete;
        _reject = reject;
        _handover = handover;
    }

    /// <summary>Lists unlink requests escalated to an admin.</summary>
    [HttpGet("unlink-requests")]
    public async Task<IActionResult> GetUnlinkRequests(CancellationToken ct)
    {
        Result<IReadOnlyList<AdminUnlinkRequestDto>> result = await _getUnlinkRequests.HandleAsync(new GetUnlinkRequestsForAdminQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Completes an escalated unlink: the link is revoked.</summary>
    [HttpPost("unlink-requests/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] AdminReasonRequest request, CancellationToken ct)
    {
        Result result = await _complete.HandleAsync(new AdminCompleteUnlinkCommand(User.GetUserId(), id, request.Reason), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Rejects an escalated unlink: the link stays active.</summary>
    [HttpPost("unlink-requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] AdminReasonRequest request, CancellationToken ct)
    {
        Result result = await _reject.HandleAsync(new AdminRejectUnlinkCommand(User.GetUserId(), id, request.Reason), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Lists all links of one learner (for the Primary handover).</summary>
    [HttpGet("support-links")]
    public async Task<IActionResult> GetLearnerLinks([FromQuery] Guid learnerId, CancellationToken ct)
    {
        Result<IReadOnlyList<AdminSupportLinkDto>> result =
            await _getLearnerLinks.HandleAsync(new GetLearnerSupportLinksForAdminQuery(learnerId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Makes another active link of a child learner the Primary one.</summary>
    [HttpPost("support-links/handover")]
    public async Task<IActionResult> Handover([FromBody] HandoverPrimaryRequest request, CancellationToken ct)
    {
        Result result = await _handover.HandleAsync(
            new AdminHandoverPrimaryCommand(User.GetUserId(), request.LearnerId, request.NewPrimaryLinkId, request.Reason), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }
}
