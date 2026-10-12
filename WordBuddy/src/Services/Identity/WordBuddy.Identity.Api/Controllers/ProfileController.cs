using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileAliasAvatar;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileTimeZone;
using WordBuddy.Identity.Application.Features.Profile.Queries.GetCurrentUser;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Api.Controllers;

/// <summary>The caller profile: current user, alias and avatar.</summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly IQueryHandler<GetCurrentUserQuery, UserDto> _getCurrentUser;
    private readonly ICommandHandler<UpdateProfileAliasAvatarCommand, UserDto> _updateProfile;
    private readonly ICommandHandler<UpdateProfileTimeZoneCommand, UserDto> _updateTimeZone;

    public ProfileController(
        IQueryHandler<GetCurrentUserQuery, UserDto> getCurrentUser,
        ICommandHandler<UpdateProfileAliasAvatarCommand, UserDto> updateProfile,
        ICommandHandler<UpdateProfileTimeZoneCommand, UserDto> updateTimeZone)
    {
        _getCurrentUser = getCurrentUser;
        _updateProfile = updateProfile;
        _updateTimeZone = updateTimeZone;
    }

    /// <summary>Returns the caller profile, including whether an active supporter exists.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        Result<UserDto> result = await _getCurrentUser.HandleAsync(new GetCurrentUserQuery(User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Sets or clears the alias and avatar.</summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        Result<UserDto> result = await _updateProfile.HandleAsync(
            new UpdateProfileAliasAvatarCommand(User.GetUserId(), request.Alias, request.AvatarId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Sets or clears the IANA time zone that defines the local day of the learner.</summary>
    [HttpPut("profile/time-zone")]
    public async Task<IActionResult> UpdateTimeZone([FromBody] UpdateTimeZoneRequest request, CancellationToken ct)
    {
        Result<UserDto> result = await _updateTimeZone.HandleAsync(
            new UpdateProfileTimeZoneCommand(User.GetUserId(), request.TimeZoneId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Returns the fixed avatar catalog.</summary>
    [HttpGet("avatars")]
    public IActionResult GetAvatars() => Ok(AvatarCatalog.Ids);
}
