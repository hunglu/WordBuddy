using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondUnlink;

/// <summary>The other side confirms (link revoked) or declines (link stays, no override possible).</summary>
public sealed record RespondUnlinkCommand(Guid ActorId, Guid LinkId, bool Confirm) : ICommand;
