using WordBuddy.Identity.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondToPendingSupporter;

/// <summary>The Primary supporter of a child approves or rejects an extra supporter.</summary>
public sealed record RespondToPendingSupporterCommand(Guid ActorId, Guid LinkId, bool Approve) : ICommand;
