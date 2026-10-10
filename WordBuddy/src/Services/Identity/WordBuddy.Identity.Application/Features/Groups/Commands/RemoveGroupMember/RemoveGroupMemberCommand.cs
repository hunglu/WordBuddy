using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.RemoveGroupMember;

/// <summary>The owner removes a pending or active member.</summary>
public sealed record RemoveGroupMemberCommand(Guid GroupId, Guid CallerId, Guid LearnerId) : ICommand;
