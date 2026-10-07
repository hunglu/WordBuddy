namespace WordBuddy.Progress.Domain;

/// <summary>
/// Daily new-word cap. One rule for every user (D-3): the backlog table, unless the user set
/// <see cref="VocabularyLearnerSettings.NewWordsPerDay"/> (any user, D-4). Pure.
/// </summary>
public sealed class NewWordCapPolicy
{
    private readonly NewWordCapOptions _options;

    /// <summary>Creates the policy with the configured backlog table.</summary>
    public NewWordCapPolicy(NewWordCapOptions options)
    {
        _options = options;
    }

    /// <summary>Returns the cap for a due <paramref name="backlog"/> and an optional user override.</summary>
    public int GetCap(int backlog, int? newWordsPerDay)
    {
        if (newWordsPerDay is { } overrideCap)
        {
            return overrideCap;
        }

        if (backlog <= _options.LowBacklogMax)
        {
            return _options.LowBacklogCap;
        }

        if (backlog <= _options.MediumBacklogMax)
        {
            return _options.MediumBacklogCap;
        }

        return backlog <= _options.HighBacklogMax ? _options.HighBacklogCap : _options.OverflowCap;
    }
}
