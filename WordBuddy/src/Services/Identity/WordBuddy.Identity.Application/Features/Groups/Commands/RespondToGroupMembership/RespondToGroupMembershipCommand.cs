using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.RespondToGroupMembership;

/// <summary>The Primary supporter of a child approves or rejects a pending group membership.</summary>
public sealed record RespondToGroupMembershipCommand(Guid MemberId, Guid CallerId, bool Approve) : ICommand;
