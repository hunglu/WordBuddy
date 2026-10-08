using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminRejectUnlink;

/// <summary>Admin rejects an escalated unlink request: the link stays active. Reason required (audited).</summary>
public sealed record AdminRejectUnlinkCommand(Guid AdminId, Guid UnlinkRequestId, string Reason) : ICommand;
