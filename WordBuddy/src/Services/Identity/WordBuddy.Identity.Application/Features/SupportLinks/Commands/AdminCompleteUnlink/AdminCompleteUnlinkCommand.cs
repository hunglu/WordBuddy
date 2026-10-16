using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminCompleteUnlink;

/// <summary>
/// Admin or moderator completes an escalated unlink request: the link is revoked. Reason required (audited).
/// <paramref name="ActorIsAdmin"/> false (a moderator) cannot act on a child learner's request. <paramref name="AdminId"/> is the actor id.
/// </summary>
public sealed record AdminCompleteUnlinkCommand(Guid AdminId, Guid UnlinkRequestId, string Reason, bool ActorIsAdmin) : ICommand;
