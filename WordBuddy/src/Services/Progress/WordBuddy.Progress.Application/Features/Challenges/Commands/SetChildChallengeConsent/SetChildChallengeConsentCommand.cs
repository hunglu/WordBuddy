using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.SetChildChallengeConsent;

/// <summary>A Guardian allows (or stops) challenges for a linked child.</summary>
public sealed record SetChildChallengeConsentCommand(Guid CallerId, Guid ChildId, bool Enabled) : ICommand;
