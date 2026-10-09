namespace WordBuddy.Content.Application.Caching;

/// <summary>Auto-fill cache keys. Both are deleted after a save or an approval of the lemma.</summary>
public static class AutofillCacheKeys
{
    /// <summary>Catalog result of a normalized lemma (absolute 1 h).</summary>
    public static string Catalog(string normalizedLemma) => $"content:autofill:{normalizedLemma}";

    /// <summary>Negative cache: the dictionary does not know the lemma (absolute 1 h).</summary>
    public static string Miss(string normalizedLemma) => $"content:autofill-miss:{normalizedLemma}";

    /// <summary>Per-sense key, deleted after an approval.</summary>
    public static string Sense(Guid senseId) => $"content:sense:{senseId}";

    /// <summary>Lifetime of both keys.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
}
