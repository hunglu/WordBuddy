namespace WordBuddy.Content.Domain;

/// <summary>Places a <see cref="Domain.Sense"/> in a <see cref="Lesson"/> at a given position
/// (formerly <c>LessonVocabularyWord</c>). One sense may belong to several lessons. Keyed by
/// (<see cref="LessonId"/>, <see cref="SenseId"/>).</summary>
public sealed class LessonSense
{
    public Guid LessonId { get; private set; }
    public Guid SenseId { get; private set; }
    public Sense? Sense { get; private set; }
    public int SortOrder { get; private set; }

    private LessonSense(Guid lessonId, Guid senseId, int sortOrder)
    {
        LessonId = lessonId;
        SenseId = senseId;
        SortOrder = sortOrder;
    }

    /// <summary>Creates a link carrying the sense itself, so adding a lesson with new senses persists both.</summary>
    public LessonSense(Guid lessonId, Sense sense, int sortOrder)
        : this(lessonId, sense.Id, sortOrder)
    {
        Sense = sense;
    }
}
