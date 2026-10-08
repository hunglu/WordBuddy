namespace WordBuddy.Progress.Domain;

/// <summary>
/// Daily new-word cap. Order: supporter cap (WB-24) → the user own
/// <see cref="VocabularyLearnerSettings.NewWordsPerDay"/> (any user, D-4) → the backlog table (D-3). Pure.
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
    public int GetCap(int backlog, int? newWordsPerDay) => GetCap(backlog, supporterCap: null, newWordsPerDay);

    /// <summary>Returns the cap: supporter cap, else own cap, else the backlog rule.</summary>
    public int GetCap(int backlog, int? supporterCap, int? newWordsPerDay)
    {
        if (supporterCap is { } supporter)
        {
            return supporter;
        }

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
