using WordBuddy.Content.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Groups.Commands.ApplyLearnerGroupEvent;

/// <summary>Applies an Identity group event (member activated or removed, or group deleted) to the local projection.</summary>
/// <param name="GroupId">The group.</param>
/// <param name="OwnerId">The group owner.</param>
/// <param name="LearnerId">The member, or <see langword="null"/> for a whole-group event (group deleted).</param>
/// <param name="IsActive">The new state of the member (always false for a whole-group event).</param>
/// <param name="OccurredAtUtc">When the event happened, UTC.</param>
public sealed record ApplyLearnerGroupEventCommand(
    Guid GroupId,
    Guid OwnerId,
    Guid? LearnerId,
    bool IsActive,
    DateTime OccurredAtUtc) : ICommand;
