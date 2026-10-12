namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>Challenge settings, bound from <c>Challenges</c>. Same for child and adult learners.</summary>
public sealed class ChallengeOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Challenges";

    /// <summary>Correct due reviews in one local day that earn the daily goal bonus. Default 10.</summary>
    public int DailyGoalCorrectDue { get; set; } = 10;

    /// <summary>Points of the daily goal bonus. Default 10.</summary>
    public int DailyGoalBonus { get; set; } = 10;

    /// <summary>Most points one learner can earn per local day. Default 200.</summary>
    public int DailyPointCap { get; set; } = 200;

    /// <summary>An answer faster than this many milliseconds earns nothing. Default 800.</summary>
    public int FastAnswerFloorMs { get; set; } = 800;

    /// <summary>Most learners in one challenge. Default 30. There is no overflow instance.</summary>
    public int Capacity { get; set; } = 30;

    /// <summary>Hours a challenge is open for joining before it starts. Default 24.</summary>
    public int OpenLeadHours { get; set; } = 24;

    /// <summary>Seconds between life cycle checks of the scheduler. Default 60.</summary>
    public int LifecycleTickSeconds { get; set; } = 60;

    /// <summary>Minutes between score recomputations of the scheduler. Default 5.</summary>
    public int RecomputeIntervalMinutes { get; set; } = 5;

    /// <summary>Whether the background scheduler runs. Default true.</summary>
    public bool SchedulerEnabled { get; set; } = true;
}
