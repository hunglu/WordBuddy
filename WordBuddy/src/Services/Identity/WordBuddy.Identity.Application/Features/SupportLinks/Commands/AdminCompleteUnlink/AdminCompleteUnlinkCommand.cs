using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminCompleteUnlink;

/// <summary>Admin completes an escalated unlink request: the link is revoked. Reason required (audited).</summary>
public sealed record AdminCompleteUnlinkCommand(Guid AdminId, Guid UnlinkRequestId, string Reason) : ICommand;
