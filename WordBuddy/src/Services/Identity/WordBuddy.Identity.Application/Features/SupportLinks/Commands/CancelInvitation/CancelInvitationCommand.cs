using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelInvitation;

/// <summary>Cancels an open invitation. Creator only.</summary>
public sealed record CancelInvitationCommand(Guid ActorId, Guid InvitationId) : ICommand;
