namespace WordBuddy.Domain.Enums;

/// <summary>Specifies the intended audience age group for a <see cref="Entities.Lesson"/>.</summary>
public enum TargetAgeGroup
{
    /// <summary>Content is designed for child learners only.</summary>
    Child,

    /// <summary>Content is designed for adult learners only.</summary>
    Adult,

    /// <summary>Content is suitable for all age groups.</summary>
    Both,
}
