using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.EscalateUnlink;

/// <summary>The requester asks an admin to override, only after the wait time without any response.</summary>
public sealed record EscalateUnlinkCommand(Guid ActorId, Guid LinkId) : ICommand;
