using Microsoft.Extensions.Caching.Distributed;

namespace WordBuddy.Content.Application.Caching;

/// <summary>Deletes the cache keys that hold a copy of lexeme fields (part of speech, IPA, audio
/// URLs). Call it after a lexeme command succeeded.</summary>
public static class LexemeCacheInvalidator
{
    /// <summary>Deletes the auto-fill catalog key of <paramref name="normalizedLemma"/> and both
    /// shared-pool keys.</summary>
    public static async Task InvalidateAsync(IDistributedCache cache, string normalizedLemma, CancellationToken ct = default)
    {
        await cache.RemoveAsync(AutofillCacheKeys.Catalog(normalizedLemma), ct);
        await cache.RemoveAsync(SharedVocabularyCacheKeys.ChildSafe, ct);
        await cache.RemoveAsync(SharedVocabularyCacheKeys.All, ct);
    }
}
