namespace WordBuddy.Content.Application.Caching;

/// <summary>Cache keys of the community word pool, one per <c>childSafeOnly</c> variant. Every
/// command that changes the pool deletes both keys.</summary>
public static class SharedVocabularyCacheKeys
{
    /// <summary>Pool as seen by Child callers (only <c>VisibleToChildren</c> words).</summary>
    public const string ChildSafe = "content:vocabulary-shared:True";

    /// <summary>Pool as seen by Adult callers (all shared words).</summary>
    public const string All = "content:vocabulary-shared:False";

    /// <summary>Returns the key for the given variant.</summary>
    public static string For(bool childSafeOnly) => childSafeOnly ? ChildSafe : All;
}
