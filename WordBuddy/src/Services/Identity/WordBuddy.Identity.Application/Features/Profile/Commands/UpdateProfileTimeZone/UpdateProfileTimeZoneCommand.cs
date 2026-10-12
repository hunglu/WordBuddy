using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileTimeZone;

/// <summary>Sets or clears the caller IANA time zone (defines the local day for challenges).</summary>
public sealed record UpdateProfileTimeZoneCommand(Guid UserId, string? TimeZoneId) : ICommand<UserDto>;
