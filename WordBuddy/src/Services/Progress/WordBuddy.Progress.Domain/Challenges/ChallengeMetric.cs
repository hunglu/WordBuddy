namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>What a challenge counts. A fixed catalog: all values are computed from <see cref="ReviewLog"/> rows only.</summary>
public enum ChallengeMetric
{
    /// <summary>Points for correct due reviews, hint-free first tries, recall upgrades and the daily goal.</summary>
    MemoryPoints,

    /// <summary>Number of local days with at least one correct due review.</summary>
    DueReviewsCorrect,

    /// <summary>Number of words that moved from recognition to recall (first correct typing).</summary>
    RecallUpgrades,

    /// <summary>Correct due listening reviews.</summary>
    ListeningCorrect
}
