using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.SetModeratorRole;

/// <summary>Admin grants or removes the Moderator role of an adult, non-admin account.</summary>
public sealed record SetModeratorRoleCommand(Guid AdminId, Guid TargetUserId, bool IsModerator) : ICommand;
