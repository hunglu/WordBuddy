using WordBuddy.Domain.Enums;

namespace WordBuddy.Domain.Entities;

/// <summary>A structured learning unit covering vocabulary, grammar, or a daily phrase.</summary>
public sealed class Lesson
{
    /// <summary>Initializes a new <see cref="Lesson"/>.</summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="title">Lesson title displayed to learners.</param>
    /// <param name="description">Short description of the lesson content.</param>
    /// <param name="type">Content category of the lesson.</param>
    /// <param name="level">Target proficiency level.</param>
    /// <param name="targetAgeGroup">Intended audience age group.</param>
    /// <param name="isPublished">Whether the lesson is visible to learners.</param>
    /// <param name="orderIndex">Display order within the lesson catalogue.</param>
    /// <param name="createdAt">UTC timestamp of lesson creation.</param>
    public Lesson(
        Guid id,
        string title,
        string description,
        LessonType type,
        Level level,
        TargetAgeGroup targetAgeGroup,
        bool isPublished,
        int orderIndex,
        DateTime createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Type = type;
        Level = level;
        TargetAgeGroup = targetAgeGroup;
        IsPublished = isPublished;
        OrderIndex = orderIndex;
        CreatedAt = createdAt;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the lesson title displayed to learners.</summary>
    public string Title { get; }

    /// <summary>Gets the short description of the lesson content.</summary>
    public string Description { get; }

    /// <summary>Gets the content category of the lesson.</summary>
    public LessonType Type { get; }

    /// <summary>Gets the target proficiency level.</summary>
    public Level Level { get; }

    /// <summary>Gets the intended audience age group.</summary>
    public TargetAgeGroup TargetAgeGroup { get; }

    /// <summary>Gets a value indicating whether the lesson is visible to learners.</summary>
    public bool IsPublished { get; }

    /// <summary>Gets the display order within the lesson catalogue.</summary>
    public int OrderIndex { get; }

    /// <summary>Gets the UTC timestamp when the lesson was created.</summary>
    public DateTime CreatedAt { get; }
}
