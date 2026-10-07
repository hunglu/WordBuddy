namespace WordBuddy.Progress.Domain;

/// <summary>Learner-facing status of one word, derived from its FSRS state (serialised as a string).</summary>
public enum WordStatus
{
    /// <summary>In the list, never reviewed.</summary>
    New,

    /// <summary>In FSRS learning or relearning steps.</summary>
    Learning,

    /// <summary>Graduated to day-based FSRS reviews.</summary>
    Review,

    /// <summary>Stability reached the mastered threshold.</summary>
    Mastered,

    /// <summary>Lapsed often enough to need extra attention. Still scheduled.</summary>
    Leech
}
