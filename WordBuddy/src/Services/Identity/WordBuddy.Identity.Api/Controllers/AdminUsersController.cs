using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.Features.Profile.Commands.RepublishLearnerProfiles;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Profile.Commands.SetGuardianRole;
using WordBuddy.Identity.Application.Features.Profile.Commands.SetModeratorRole;
using WordBuddy.Identity.Application.Features.Users.Queries.GetUsersForAdmin;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Controllers;

/// <summary>Admin user operations: user list, Guardian and Moderator roles, and the profile backfill.</summary>
[ApiController]
[Route("api/auth/admin")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly ICommandHandler<SetGuardianRoleCommand> _setGuardian;
    private readonly ICommandHandler<SetModeratorRoleCommand> _setModerator;
    private readonly IQueryHandler<GetUsersForAdminQuery, PagedResult<AdminUserDto>> _getUsers;
    private readonly ICommandHandler<RepublishLearnerProfilesCommand, int> _republish;

    public AdminUsersController(
        ICommandHandler<SetGuardianRoleCommand> setGuardian,
        ICommandHandler<SetModeratorRoleCommand> setModerator,
        IQueryHandler<GetUsersForAdminQuery, PagedResult<AdminUserDto>> getUsers,
        ICommandHandler<RepublishLearnerProfilesCommand, int> republish)
    {
        _setGuardian = setGuardian;
        _setModerator = setModerator;
        _getUsers = getUsers;
        _republish = republish;
    }

    /// <summary>Grants or removes the Guardian role of an adult account.</summary>
    [HttpPut("users/{id:guid}/guardian")]
    public async Task<IActionResult> SetGuardian(Guid id, [FromBody] SetGuardianRequest request, CancellationToken ct)
    {
        Result result = await _setGuardian.HandleAsync(new SetGuardianRoleCommand(User.GetUserId(), id, request.IsGuardian), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Grants or removes the Moderator role of an adult, non-admin account. Shows in the token after the next sign-in.</summary>
    [HttpPut("users/{id:guid}/moderator")]
    public async Task<IActionResult> SetModerator(Guid id, [FromBody] SetModeratorRequest request, CancellationToken ct)
    {
        Result result = await _setModerator.HandleAsync(new SetModeratorRoleCommand(User.GetUserId(), id, request.IsModerator), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>Lists one page of adult accounts for role assignment. <c>search</c> matches email or display name.</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken ct = default)
    {
        Result<PagedResult<AdminUserDto>> result = await _getUsers.HandleAsync(new GetUsersForAdminQuery(search, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>One-off backfill: publishes <c>LearnerProfileUpdated</c> for all existing users. Safe to rerun.</summary>
    [HttpPost("learner-profiles/republish")]
    public async Task<IActionResult> Republish(CancellationToken ct)
    {
        Result<int> result = await _republish.HandleAsync(new RepublishLearnerProfilesCommand(), ct);
        return result.IsSuccess ? Ok(new { published = result.Value }) : result.ToProblemResult(this);
    }
}
