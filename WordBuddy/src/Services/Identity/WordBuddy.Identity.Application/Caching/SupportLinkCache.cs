using Microsoft.Extensions.Caching.Distributed;

namespace WordBuddy.Identity.Application.Caching;

/// <summary>Cache keys of the per-user link list (<c>identity:supportlinks:{userId}</c>).
/// Every link command deletes the key of every affected user.</summary>
public static class SupportLinkCache
{
    /// <summary>Returns the key for <paramref name="userId"/>.</summary>
    public static string Key(Guid userId) => $"identity:supportlinks:{userId}";

    /// <summary>Deletes the keys of the given users (nulls and duplicates are skipped).</summary>
    public static async Task InvalidateAsync(IDistributedCache cache, CancellationToken ct, params Guid?[] userIds)
    {
        foreach (Guid userId in userIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct())
        {
            await cache.RemoveAsync(Key(userId), ct);
        }
    }
}
