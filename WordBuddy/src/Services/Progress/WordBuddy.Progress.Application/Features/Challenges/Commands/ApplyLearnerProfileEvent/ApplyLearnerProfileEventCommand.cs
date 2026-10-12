using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.ApplyLearnerProfileEvent;

/// <summary>Applies an Identity <c>LearnerProfileUpdated</c> snapshot to the local <c>ChallengeProfile</c>.</summary>
public sealed record ApplyLearnerProfileEventCommand(
    Guid UserId,
    string AgeGroup,
    string? Alias,
    string? AvatarId,
    string? DisplayName,
    string? TimeZoneId,
    bool IsGuardian,
    DateTime OccurredAtUtc) : ICommand;
