namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>How a challenge is decided.</summary>
public enum ChallengeKind
{
    /// <summary>Learners are ranked by score.</summary>
    Ranked,

    /// <summary>Learners try to reach a fixed target.</summary>
    Target
}
