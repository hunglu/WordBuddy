namespace WordBuddy.Shared.Contracts.Groups;

/// <summary>
/// Published by Identity when a group is deleted. Consumers deactivate every member of the group.
/// Payload rule: ids and time only.
/// </summary>
/// <param name="GroupId">The group (scope key).</param>
/// <param name="OwnerId">The supporter who owned the group.</param>
/// <param name="OccurredAtUtc">When the group was deleted, UTC.</param>
public sealed record LearnerGroupDeleted(Guid GroupId, Guid OwnerId, DateTime OccurredAtUtc);
