using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileAliasAvatar;

/// <summary>Sets or clears the caller alias and avatar.</summary>
public sealed record UpdateProfileAliasAvatarCommand(Guid UserId, string? Alias, string? AvatarId) : ICommand<UserDto>;
