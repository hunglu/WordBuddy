using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WordBuddy.Progress.Api.Authorization;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;
using WordBuddy.Progress.Application.Features.Groups.Queries.GetGroupDashboard;
using WordBuddy.Progress.Application.Features.VocabularySrs;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Api.Controllers;

/// <summary>Learner dashboard (WB-26): the learner's own view and the supporter view. Read-only, computed
/// from review logs. Every active supporter gets the full view (WB-24). Dates only — never a time of day.
/// A child without an active supporter gets 403 <c>Learner.SupporterRequired</c> on <c>me</c>.</summary>
[ApiController]
[Route("api/progress/dashboard")]
[Authorize]
[EnableRateLimiting(RateLimitingConfiguration.DashboardPolicy)]
public sealed class DashboardController : ControllerBase
{
    private readonly IQueryHandler<GetLearnerDashboardQuery, LearnerDashboardDto> _getDashboard;
    private readonly IQueryHandler<GetGroupDashboardQuery, GroupDashboardDto> _getGroupDashboard;
    private readonly DashboardOptions _options;

    public DashboardController(
        IQueryHandler<GetLearnerDashboardQuery, LearnerDashboardDto> getDashboard,
        IQueryHandler<GetGroupDashboardQuery, GroupDashboardDto> getGroupDashboard,
        DashboardOptions options)
    {
        _getDashboard = getDashboard;
        _getGroupDashboard = getGroupDashboard;
        _options = options;
    }

    /// <summary>Gets the caller's own dashboard. <c>days</c> is 7, 30 or 90 (default from options).
    /// The <c>X-Client-CurrentDateTime</c> header sets the client's day (offset only); missing → UTC.</summary>
    [HttpGet("me")]
    [Authorize(Policy = SupportLinkPolicies.ChildHasSupporter)]
    public async Task<IActionResult> GetMine(
        [FromQuery] int? days,
        [FromHeader(Name = ClientDateTime.HeaderName)] string? clientCurrentDateTime,
        CancellationToken ct)
    {
        Result<LearnerDashboardDto> result = await _getDashboard.HandleAsync(
            new GetLearnerDashboardQuery(User.GetUserId(), clientCurrentDateTime, days ?? _options.DefaultDays), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Gets a learner's dashboard. Caller must be an active supporter of the learner.</summary>
    [HttpGet("learners/{learnerId:guid}")]
    [Authorize(Policy = SupportLinkPolicies.CanSupportLearner)]
    public async Task<IActionResult> GetLearner(
        Guid learnerId,
        [FromQuery] int? days,
        [FromHeader(Name = ClientDateTime.HeaderName)] string? clientCurrentDateTime,
        CancellationToken ct)
    {
        Result<LearnerDashboardDto> result = await _getDashboard.HandleAsync(
            new GetLearnerDashboardQuery(learnerId, clientCurrentDateTime, days ?? _options.DefaultDays), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Gets the dashboard of a learning group (WB-27): one row per active member. Caller must own the
    /// group (<c>CanManageGroup</c>), otherwise 403. <c>days</c> is 7, 30 or 90. Members whose support link to the
    /// caller ended are left out.</summary>
    [HttpGet("groups/{groupId:guid}")]
    [Authorize(Policy = GroupPolicies.CanManageGroup)]
    public async Task<IActionResult> GetGroup(
        Guid groupId,
        [FromQuery] int? days,
        [FromHeader(Name = ClientDateTime.HeaderName)] string? clientCurrentDateTime,
        CancellationToken ct)
    {
        Result<GroupDashboardDto> result = await _getGroupDashboard.HandleAsync(
            new GetGroupDashboardQuery(groupId, User.GetUserId(), clientCurrentDateTime, days ?? _options.DefaultDays), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }
}
