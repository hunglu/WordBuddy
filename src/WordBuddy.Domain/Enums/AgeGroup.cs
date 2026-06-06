namespace WordBuddy.Domain.Enums;

/// <summary>Classifies a user by age group, used to enforce content restrictions.</summary>
public enum AgeGroup
{
    /// <summary>Learner is a child (typically under 13); subject to additional content restrictions.</summary>
    Child,

    /// <summary>Learner is an adult.</summary>
    Adult,
}
