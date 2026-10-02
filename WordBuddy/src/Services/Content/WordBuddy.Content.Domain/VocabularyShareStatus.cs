namespace WordBuddy.Content.Domain;

/// <summary>Moderation lifecycle of a <see cref="VocabularyWord"/>'s sharing state.</summary>
public enum VocabularyShareStatus
{
    Private,
    PendingReview,
    Shared,
    Rejected
}
