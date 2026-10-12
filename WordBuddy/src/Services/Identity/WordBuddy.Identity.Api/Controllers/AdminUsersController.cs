using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.Features.Profile.Commands.RepublishLearnerProfiles;
using WordBuddy.Identity.Application.Features.Profile.Commands.SetGuardianRole;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Controllers;

/// <summary>Admin user operations: Guardian role and the profile backfill.</summary>
[ApiController]
[Route("api/auth/admin")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly ICommandHandler<SetGuardianRoleCommand> _setGuardian;
    private readonly ICommandHandler<RepublishLearnerProfilesCommand, int> _republish;

    public AdminUsersController(
        ICommandHandler<SetGuardianRoleCommand> setGuardian,
        ICommandHandler<RepublishLearnerProfilesCommand, int> republish)
    {
        _setGuardian = setGuardian;
        _republish = republish;
    }

    /// <summary>Grants or removes the Guardian role of an adult account.</summary>
    [HttpPut("users/{id:guid}/guardian")]
    public async Task<IActionResult> SetGuardian(Guid id, [FromBody] SetGuardianRequest request, CancellationToken ct)
    {
        Result result = await _setGuardian.HandleAsync(new SetGuardianRoleCommand(User.GetUserId(), id, request.IsGuardian), ct);
        return result.IsSuccess ? NoContent() : result.ToProblemResult(this);
    }

    /// <summary>One-off backfill: publishes <c>LearnerProfileUpdated</c> for all existing users. Safe to rerun.</summary>
    [HttpPost("learner-profiles/republish")]
    public async Task<IActionResult> Republish(CancellationToken ct)
    {
        Result<int> result = await _republish.HandleAsync(new RepublishLearnerProfilesCommand(), ct);
        return result.IsSuccess ? Ok(new { published = result.Value }) : result.ToProblemResult(this);
    }
}
