using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminHandoverPrimary;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetLearnerSupportLinksForAdmin;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Controllers;

/// <summary>Admin-only Primary handover and learner link lookup. This flow concerns child learners, so moderators cannot use it.</summary>
[ApiController]
[Route("api/auth/admin")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminSupportLinkHandoverController : ControllerBase
{
    private readonly IQueryHandler<GetLearnerSupportLinksForAdminQuery, PagedResult<AdminSupportLinkDto>> _getLearnerLinks;
    private readonly ICommandHandler<AdminHandoverPrimaryCommand> _handover;

    public AdminSupportLinkHandoverController(
        IQueryHandler<GetLearnerSupportLinksForAdminQuery, PagedResult<AdminSupportLinkDto>> getLearnerLinks,
        ICommandHandler<AdminHandoverPrimaryCommand> handover)
    {
        _getLearnerLinks = getLearnerLinks;
        _handover = handover;
    }

    /// <summary>Lists one page of the links of one learner (for the Primary handover).</summary>
    [HttpGet("support-links")]
    public async Task<IActionResult> GetLearnerLinks(
        [FromQuery] Guid learnerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken ct = default)
    {
        Result<PagedResult<AdminSupportLinkDto>> result =
            await _getLearnerLinks.HandleAsync(new GetLearnerSupportLinksForAdminQuery(learnerId, page, pageSize), ct);
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
