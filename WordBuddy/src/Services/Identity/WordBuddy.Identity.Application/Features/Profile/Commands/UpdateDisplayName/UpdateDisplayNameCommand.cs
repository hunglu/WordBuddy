using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.UpdateDisplayName;

/// <summary>Changes the caller display name (2 to 50 characters).</summary>
public sealed record UpdateDisplayNameCommand(Guid UserId, string DisplayName) : ICommand<UserDto>;
