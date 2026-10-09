namespace WordBuddy.Progress.Domain;

/// <summary>Dashboard thresholds, bound from <c>Dashboard</c>. Same for child and adult learners.</summary>
public sealed class DashboardOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Dashboard";

    /// <summary>Window in days when the caller sends none. Default 30.</summary>
    public int DefaultDays { get; set; } = 30;

    /// <summary>Fewest answers needed before a retention figure is shown. Default 10.</summary>
    public int MinSample { get; set; } = 10;

    /// <summary>A wrong answer faster than this many milliseconds counts as "very fast wrong". Default 1500.</summary>
    public int FastWrongMs { get; set; } = 1500;

    /// <summary>Hint rate (0–1) above which the hint signal is flagged. Default 0.3.</summary>
    public double HintRateFlag { get; set; } = 0.3;
}
