namespace WordBuddy.Progress.Domain;

/// <summary>FSRS card state (serialised as a string).</summary>
public enum FsrsPhase
{
    /// <summary>Short learning steps before the first day-based interval.</summary>
    Learning = 1,

    /// <summary>Day-based intervals.</summary>
    Review = 2,

    /// <summary>Short relearning steps after a lapse.</summary>
    Relearning = 3
}
