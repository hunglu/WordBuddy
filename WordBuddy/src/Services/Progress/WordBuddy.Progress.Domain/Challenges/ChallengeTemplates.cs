namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>The static launch set of challenge templates.</summary>
public static class ChallengeTemplates
{
    /// <summary>Key of the weekly league.</summary>
    public const string WeeklyLeagueKey = "weekly-league";

    /// <summary>Key of the 7-day memory challenge.</summary>
    public const string SevenDayMemoryKey = "seven-day-memory";

    /// <summary>Key of the word climber.</summary>
    public const string WordClimberKey = "word-climber";

    /// <summary>Key of the listening sprint (adults only).</summary>
    public const string ListeningSprintKey = "listening-sprint";

    private static readonly ChallengeAudience[] Both = [ChallengeAudience.Adult, ChallengeAudience.Child];

    /// <summary>All templates.</summary>
    public static IReadOnlyList<ChallengeTemplate> All { get; } =
    [
        new(WeeklyLeagueKey, "Weekly League", ChallengeKind.Ranked, ChallengeMetric.MemoryPoints, null, TimeSpan.FromDays(7), true, Both),
        new(SevenDayMemoryKey, "7-Day Memory", ChallengeKind.Target, ChallengeMetric.DueReviewsCorrect, 7, TimeSpan.FromDays(7), false, Both),
        new(WordClimberKey, "Word Climber", ChallengeKind.Target, ChallengeMetric.RecallUpgrades, 20, TimeSpan.FromDays(14), false, Both),
        new(ListeningSprintKey, "Listening Sprint", ChallengeKind.Ranked, ChallengeMetric.ListeningCorrect, null, TimeSpan.FromDays(3), false, [ChallengeAudience.Adult]),
    ];

    /// <summary>Finds a template by key, or null.</summary>
    public static ChallengeTemplate? Find(string key) => All.FirstOrDefault(t => t.Key == key);

    /// <summary>The display name for a key. Unknown key: the key itself.</summary>
    public static string NameOf(string key) => Find(key)?.Name ?? key;
}
