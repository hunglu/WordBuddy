namespace WordBuddy.Content.Domain;

/// <summary>Places a <see cref="Domain.VocabularyWord"/> in a <see cref="Lesson"/> at a given
/// position. One word may belong to several lessons. Keyed by (<see cref="LessonId"/>,
/// <see cref="VocabularyWordId"/>).</summary>
public sealed class LessonVocabularyWord
{
    public Guid LessonId { get; private set; }
    public Guid VocabularyWordId { get; private set; }
    public VocabularyWord? VocabularyWord { get; private set; }
    public int SortOrder { get; private set; }

    private LessonVocabularyWord(Guid lessonId, Guid vocabularyWordId, int sortOrder)
    {
        LessonId = lessonId;
        VocabularyWordId = vocabularyWordId;
        SortOrder = sortOrder;
    }

    /// <summary>Creates a link carrying the word itself, so adding a lesson with new words persists both.</summary>
    public LessonVocabularyWord(Guid lessonId, VocabularyWord vocabularyWord, int sortOrder)
        : this(lessonId, vocabularyWord.Id, sortOrder)
    {
        VocabularyWord = vocabularyWord;
    }
}
