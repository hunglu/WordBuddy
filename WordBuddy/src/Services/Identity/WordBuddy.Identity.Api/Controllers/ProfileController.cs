using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WordBuddy.Identity.Api.Extensions;
using WordBuddy.Identity.Api.Models;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Profile.Commands.ChangePassword;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDateOfBirth;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDisplayName;
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
    private readonly ICommandHandler<UpdateDateOfBirthCommand, AuthTokenDto> _updateDateOfBirth;
    private readonly ICommandHandler<UpdateDisplayNameCommand, UserDto> _updateDisplayName;
    private readonly ICommandHandler<ChangePasswordCommand, AuthTokenDto> _changePassword;

    public ProfileController(
        IQueryHandler<GetCurrentUserQuery, UserDto> getCurrentUser,
        ICommandHandler<UpdateProfileAliasAvatarCommand, UserDto> updateProfile,
        ICommandHandler<UpdateProfileTimeZoneCommand, UserDto> updateTimeZone,
        ICommandHandler<UpdateDateOfBirthCommand, AuthTokenDto> updateDateOfBirth,
        ICommandHandler<UpdateDisplayNameCommand, UserDto> updateDisplayName,
        ICommandHandler<ChangePasswordCommand, AuthTokenDto> changePassword)
    {
        _getCurrentUser = getCurrentUser;
        _updateProfile = updateProfile;
        _updateTimeZone = updateTimeZone;
        _updateDateOfBirth = updateDateOfBirth;
        _updateDisplayName = updateDisplayName;
        _changePassword = changePassword;
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

    /// <summary>Sets or clears the caller date of birth. Returns a new token so the age band claim refreshes.</summary>
    [HttpPut("profile/date-of-birth")]
    public async Task<IActionResult> UpdateDateOfBirth([FromBody] UpdateDateOfBirthRequest request, CancellationToken ct)
    {
        Result<AuthTokenDto> result = await _updateDateOfBirth.HandleAsync(
            new UpdateDateOfBirthCommand(User.GetUserId(), request.DateOfBirth), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Changes the caller display name.</summary>
    [HttpPut("profile/display-name")]
    public async Task<IActionResult> UpdateDisplayName([FromBody] UpdateDisplayNameRequest request, CancellationToken ct)
    {
        Result<UserDto> result = await _updateDisplayName.HandleAsync(
            new UpdateDisplayNameCommand(User.GetUserId(), request.DisplayName), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Changes the caller password (current password required). Returns a fresh token.</summary>
    [HttpPost("profile/password")]
    [EnableRateLimiting(RateLimitingConfiguration.PasswordChangePolicy)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        Result<AuthTokenDto> result = await _changePassword.HandleAsync(
            new ChangePasswordCommand(User.GetUserId(), request.CurrentPassword, request.NewPassword), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Returns the fixed avatar catalog.</summary>
    [HttpGet("avatars")]
    public IActionResult GetAvatars() => Ok(AvatarCatalog.Ids);
}
