using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.SupportLinks.Commands.ApplySupportLinkEvent;

/// <summary>Applies an Identity link event (activated or revoked) to the local projection.</summary>
public sealed record ApplySupportLinkEventCommand(
    Guid LinkId,
    Guid LearnerId,
    Guid SupporterId,
    bool IsActive,
    DateTime OccurredAtUtc) : ICommand;
