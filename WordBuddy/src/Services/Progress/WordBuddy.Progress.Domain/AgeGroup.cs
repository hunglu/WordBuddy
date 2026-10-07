namespace WordBuddy.Progress.Domain;

/// <summary>The caller's age group, read from the JWT <c>age_group</c> claim (owned by Identity).</summary>
public enum AgeGroup
{
    /// <summary>Child account.</summary>
    Child,

    /// <summary>Adult account.</summary>
    Adult
}
