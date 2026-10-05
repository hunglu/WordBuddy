namespace WordBuddy.Content.Domain;

/// <summary>Moderation lifecycle of a <see cref="Sense"/>'s sharing state.</summary>
public enum VocabularyShareStatus
{
    Private,
    PendingReview,
    Shared,
    Rejected
}
