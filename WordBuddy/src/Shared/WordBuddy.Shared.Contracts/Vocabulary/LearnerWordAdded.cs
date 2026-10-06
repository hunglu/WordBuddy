namespace WordBuddy.Shared.Contracts.Vocabulary;

/// <summary>
/// Published by Content when a learner gets a word sense in their list (own add or adopt).
/// Payload rule: ids and timestamps only — never word text, personal context, names, or age group.
/// </summary>
/// <param name="UserId">Learner who now has the sense in their list.</param>
/// <param name="SenseId">The linked sense.</param>
/// <param name="AddedBy">User who caused the link (the learner today).</param>
/// <param name="AddedAtUtc">When the link was created, UTC.</param>
public sealed record LearnerWordAdded(Guid UserId, Guid SenseId, Guid AddedBy, DateTime AddedAtUtc);
