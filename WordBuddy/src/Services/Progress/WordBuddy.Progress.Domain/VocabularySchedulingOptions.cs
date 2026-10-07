namespace WordBuddy.Progress.Domain;

/// <summary>Scheduling options, bound from <c>Vocabulary:Scheduling</c>.</summary>
public sealed class VocabularySchedulingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Vocabulary:Scheduling";

    /// <summary>FSRS target recall probability. Default 0.9.</summary>
    public double DesiredRetention { get; set; } = 0.9;

    /// <summary>Longest interval in days. Default 36500.</summary>
    public int MaximumIntervalDays { get; set; } = 36500;

    /// <summary>Learning steps in minutes. <see langword="null"/> = FSRS default (1 and 10).</summary>
    public int[]? LearningStepsMinutes { get; set; }

    /// <summary>Relearning steps in minutes. <see langword="null"/> = FSRS default (10).</summary>
    public int[]? RelearningStepsMinutes { get; set; }

    /// <summary>Stability (days) at which a word counts as mastered. Default 21.</summary>
    public double MasteredStabilityDays { get; set; } = 21;

    /// <summary>Lapses at which a word becomes a leech. Default 4.</summary>
    public int LeechLapses { get; set; } = 4;

    /// <summary>Most due items one session returns. Default 50.</summary>
    public int MaxDueItems { get; set; } = 50;

    /// <summary>Learning steps as time spans, with the FSRS default when not configured.</summary>
    public IReadOnlyList<TimeSpan> GetLearningSteps() =>
        (LearningStepsMinutes ?? [1, 10]).Select(m => TimeSpan.FromMinutes(m)).ToList();

    /// <summary>Relearning steps as time spans, with the FSRS default when not configured.</summary>
    public IReadOnlyList<TimeSpan> GetRelearningSteps() =>
        (RelearningStepsMinutes ?? [10]).Select(m => TimeSpan.FromMinutes(m)).ToList();
}
