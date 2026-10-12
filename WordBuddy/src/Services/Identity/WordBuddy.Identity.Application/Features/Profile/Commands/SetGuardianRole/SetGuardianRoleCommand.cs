using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.SetGuardianRole;

/// <summary>Admin grants or removes the Guardian role of an adult account.</summary>
public sealed record SetGuardianRoleCommand(Guid AdminId, Guid TargetUserId, bool IsGuardian) : ICommand;
