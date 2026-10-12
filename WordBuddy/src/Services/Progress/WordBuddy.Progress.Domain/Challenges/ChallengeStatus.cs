namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>Challenge life cycle. Moves forward only: Scheduled, Open, Running, Finished.</summary>
public enum ChallengeStatus
{
    /// <summary>Created, not yet open for joining.</summary>
    Scheduled,

    /// <summary>Open for joining, not yet started.</summary>
    Open,

    /// <summary>Started: scores are counted. Joining stays possible while there is room.</summary>
    Running,

    /// <summary>Over: scores are final.</summary>
    Finished
}
