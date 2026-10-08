using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelUnlink;

/// <summary>The requester withdraws the open unlink request of a link.</summary>
public sealed record CancelUnlinkCommand(Guid ActorId, Guid LinkId) : ICommand;
