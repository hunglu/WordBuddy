namespace WordBuddy.Content.Domain;

/// <summary>Moderation lifecycle of a <see cref="PersonalVocabularyWord"/>'s sharing state.</summary>
public enum VocabularyShareStatus
{
    Private,
    PendingReview,
    Shared,
    Rejected
}
