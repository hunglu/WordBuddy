namespace WordBuddy.Content.Api.Models;

/// <summary>Request body for an admin's approve/reject decision on a pending-review word.</summary>
public sealed record ModerateVocabularyWordRequest(bool Approve, bool VisibleToChildren);
