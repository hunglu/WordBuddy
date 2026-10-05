namespace WordBuddy.Shared.Contracts.Vocabulary;

/// <summary>
/// Published by Content when a learner's link to a word sense is removed (delete, hand-over,
/// orphan clean-up). Payload rule: ids and timestamps only — never word text, personal context,
/// names, or age group.
/// </summary>
/// <param name="UserId">Learner who lost the sense from their list.</param>
/// <param name="SenseId">The unlinked sense.</param>
/// <param name="RemovedAtUtc">When the link was removed, UTC.</param>
public sealed record LearnerWordRemoved(Guid UserId, Guid SenseId, DateTime RemovedAtUtc);
