namespace WordBuddy.Shared.Contracts.SupportLinks;

/// <summary>
/// Published by Identity when a support link becomes active.
/// Payload rule: ids and time only — never role, label, names, alias, age group or email.
/// </summary>
/// <param name="LinkId">The support link.</param>
/// <param name="LearnerId">The supported learner.</param>
/// <param name="SupporterId">The adult supporter.</param>
/// <param name="OccurredAtUtc">When the link became active, UTC.</param>
public sealed record SupportLinkActivated(Guid LinkId, Guid LearnerId, Guid SupporterId, DateTime OccurredAtUtc);
