namespace WordBuddy.Content.Domain;

/// <summary>Where a <see cref="VocabularyWord"/> came from.</summary>
public enum VocabularySource
{
    /// <summary>An admin-authored lesson word, owned by <see cref="SystemOwner.UserId"/>. Its id
    /// names its audio blob (<c>vocab-{id}-{locale}.mp3</c>), so system words are never merged.</summary>
    System,

    /// <summary>A word a learner wrote themselves.</summary>
    Learner
}
