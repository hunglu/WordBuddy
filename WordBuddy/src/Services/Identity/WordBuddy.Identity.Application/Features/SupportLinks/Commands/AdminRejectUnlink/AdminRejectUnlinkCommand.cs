using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminRejectUnlink;

/// <summary>
/// Admin or moderator rejects an escalated unlink request: the link stays active. Reason required (audited).
/// <paramref name="ActorIsAdmin"/> false (a moderator) cannot act on a child learner's request. <paramref name="AdminId"/> is the actor id.
/// </summary>
public sealed record AdminRejectUnlinkCommand(Guid AdminId, Guid UnlinkRequestId, string Reason, bool ActorIsAdmin) : ICommand;
