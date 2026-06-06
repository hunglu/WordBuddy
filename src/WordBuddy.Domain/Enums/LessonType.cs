namespace WordBuddy.Domain.Enums;

/// <summary>Specifies the category of content covered by a <see cref="Entities.Lesson"/>.</summary>
public enum LessonType
{
    /// <summary>Lesson focuses on word definitions, usage, and pronunciation.</summary>
    Vocabulary,

    /// <summary>Lesson covers grammar rules and sentence structure.</summary>
    Grammar,

    /// <summary>Lesson presents a curated phrase for daily learning.</summary>
    DailyPhrase,
}
