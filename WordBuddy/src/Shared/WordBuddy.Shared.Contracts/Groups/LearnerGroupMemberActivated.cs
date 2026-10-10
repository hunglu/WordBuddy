namespace WordBuddy.Shared.Contracts.Groups;

/// <summary>
/// Published by Identity when a learner becomes an active member of a group.
/// Payload rule: ids and time only — never names, alias, age group or email.
/// </summary>
/// <param name="GroupId">The group (scope key).</param>
/// <param name="OwnerId">The supporter who owns the group.</param>
/// <param name="LearnerId">The learner who became an active member.</param>
/// <param name="OccurredAtUtc">When the membership became active, UTC.</param>
public sealed record LearnerGroupMemberActivated(Guid GroupId, Guid OwnerId, Guid LearnerId, DateTime OccurredAtUtc);
