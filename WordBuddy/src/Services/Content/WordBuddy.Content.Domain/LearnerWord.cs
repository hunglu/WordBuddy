using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>Links a learner to a <see cref="Domain.Sense"/> in their personal list (formerly
/// <c>UserVocabularyWord</c>). A learner's list ("mine", recall check) is exactly their links.
/// Per-user settings for a word belong here.</summary>
public sealed class LearnerWord : Entity
{
    /// <summary>Maximum length of <see cref="PersonalContext"/>.</summary>
    public const int PersonalContextMaxLength = 500;

    /// <summary>The learner's user id — a plain field, not a foreign key (Content never references
    /// Identity's data).</summary>
    public Guid UserId { get; private set; }

    /// <summary>The linked sense (the former vocabulary word id).</summary>
    public Guid SenseId { get; private set; }

    /// <summary>Navigation to the linked sense.</summary>
    public Sense? Sense { get; private set; }

    /// <summary>When the learner added the word to their list (UTC).</summary>
    public DateTime AddedAtUtc { get; private set; }

    /// <summary><see langword="true"/> when the learner wrote the word; <see langword="false"/> when
    /// they adopted a shared word or matched a system word.</summary>
    public bool IsAuthor { get; private set; }

    /// <summary>Who put the word on the list. Only <see cref="LearnerWordAddedBy.Learner"/> is
    /// written today.</summary>
    public LearnerWordAddedBy AddedBy { get; private set; }

    /// <summary>The learner's own note on where they met the word. Private: never logged, shared
    /// or put in an event. Always <see langword="null"/> today.</summary>
    public string? PersonalContext { get; private set; }

    /// <summary>Creates a link added by the learner, with no personal context.</summary>
    public LearnerWord(Guid id, Guid userId, Guid senseId, bool isAuthor)
        : base(id)
    {
        UserId = userId;
        SenseId = senseId;
        IsAuthor = isAuthor;
        AddedBy = LearnerWordAddedBy.Learner;
        PersonalContext = null;
        AddedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Creates a link carrying the sense itself.</summary>
    public LearnerWord(Guid id, Guid userId, Sense sense, bool isAuthor)
        : this(id, userId, sense.Id, isAuthor)
    {
        Sense = sense;
    }
}
