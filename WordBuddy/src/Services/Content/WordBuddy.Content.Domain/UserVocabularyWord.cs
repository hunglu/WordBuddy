using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>Links a learner to a <see cref="VocabularyWord"/> in their personal list. A learner's
/// list ("mine", recall check) is exactly their links. Per-user settings for a word belong here.</summary>
public sealed class UserVocabularyWord : Entity
{
    /// <summary>The learner's user id — a plain field, not a foreign key (Content never references
    /// Identity's data).</summary>
    public Guid UserId { get; private set; }

    public Guid VocabularyWordId { get; private set; }
    public VocabularyWord? VocabularyWord { get; private set; }

    public DateTime AddedAtUtc { get; private set; }

    /// <summary><see langword="true"/> when the learner wrote the word; <see langword="false"/> when
    /// they adopted a shared word or matched a system word.</summary>
    public bool IsAuthor { get; private set; }

    public UserVocabularyWord(Guid id, Guid userId, Guid vocabularyWordId, bool isAuthor)
        : base(id)
    {
        UserId = userId;
        VocabularyWordId = vocabularyWordId;
        IsAuthor = isAuthor;
        AddedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Creates a link carrying the word itself.</summary>
    public UserVocabularyWord(Guid id, Guid userId, VocabularyWord vocabularyWord, bool isAuthor)
        : this(id, userId, vocabularyWord.Id, isAuthor)
    {
        VocabularyWord = vocabularyWord;
    }
}
