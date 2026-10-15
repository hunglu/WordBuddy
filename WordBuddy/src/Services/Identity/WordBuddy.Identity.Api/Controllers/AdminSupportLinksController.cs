using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminCompleteUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminRejectUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Controllers;

/// <summary>Admin or adult-moderator override of escalated unlinks. Moderators only see and act on adult learners. Every action is audited with a reason.</summary>
[ApiController]
[Route("api/auth/admin")]
[Authorize(Policy = AuthorizationPolicies.ManageSupportLinks)]
public sealed class AdminSupportLinksController : ControllerBase
{
    private readonly IQueryHandler<GetUnlinkRequestsForAdminQuery, PagedResult<AdminUnlinkRequestDto>> _getUnlinkRequests;
    private readonly ICommandHandler<AdminCompleteUnlinkCommand> _complete;
    private readonly ICommandHandler<AdminRejectUnlinkCommand> _reject;

    public AdminSupportLinksController(
        IQueryHandler<GetUnlinkRequestsForAdminQuery, PagedResult<AdminUnlinkRequestDto>> getUnlinkRequests,
        ICommandHandler<AdminCompleteUnlinkCommand> complete,
        ICommandHandler<AdminRejectUnlinkCommand> reject)
    {
        _getUnlinkRequests = getUnlinkRequests;
        _complete = complete;
        _reject = reject;
    }

    /// <summary>Lists one page of the unlink requests escalated to an admin.</summary>
    [HttpGet("unlink-requests")]
    public async Task<IActionResult> GetUnlinkRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken ct = default)
    {
        Result<PagedResult<AdminUnlinkRequestDto>> result = await _getUnlinkRequests.HandleAsync(
            new GetUnlinkRequestsForAdminQuery(page, pageSize, User.IsAdmin()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Completes an escalated unlink: the link is revoked.</summary>
    [HttpPost("unlink-requests/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] AdminReasonRequest request, CancellationToken ct)
    {
        Result result = await _complete.HandleAsync(new AdminCompleteUnlinkCommand(User.GetUserId(), id, request.Reason, User.IsAdmin()), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Rejects an escalated unlink: the link stays active.</summary>
    [HttpPost("unlink-requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] AdminReasonRequest request, CancellationToken ct)
    {
        Result result = await _reject.HandleAsync(new AdminRejectUnlinkCommand(User.GetUserId(), id, request.Reason, User.IsAdmin()), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }
}
