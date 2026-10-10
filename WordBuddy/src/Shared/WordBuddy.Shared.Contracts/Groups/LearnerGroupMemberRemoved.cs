namespace WordBuddy.Shared.Contracts.Groups;

/// <summary>
/// Published by Identity when a member leaves or is removed from a group.
/// Payload rule: ids, reason code and time only — never names, alias, age group or email.
/// </summary>
/// <param name="GroupId">The group (scope key).</param>
/// <param name="OwnerId">The supporter who owns the group.</param>
/// <param name="LearnerId">The learner who is no longer a member.</param>
/// <param name="Reason">Reason code: ByOwner, LinkRevoked, Left, RejectedByPrimary or GroupDeleted.</param>
/// <param name="OccurredAtUtc">When the member was removed, UTC.</param>
public sealed record LearnerGroupMemberRemoved(Guid GroupId, Guid OwnerId, Guid LearnerId, string Reason, DateTime OccurredAtUtc);
