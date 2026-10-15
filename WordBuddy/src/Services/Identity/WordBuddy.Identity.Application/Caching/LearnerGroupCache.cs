using Microsoft.Extensions.Caching.Distributed;

namespace WordBuddy.Identity.Application.Caching;

/// <summary>Cache keys of the group snapshot (<c>identity:group-snapshot:{groupId}</c>): the group and all
/// its pending and active members. Every command on a group deletes its key after a successful save.</summary>
public static class LearnerGroupCache
{
    /// <summary>Returns the key for <paramref name="groupId"/>.</summary>
    public static string Key(Guid groupId) => $"identity:group-snapshot:{groupId}";

    /// <summary>Deletes the keys of the given groups (duplicates are skipped).</summary>
    public static async Task InvalidateAsync(IDistributedCache cache, IEnumerable<Guid> groupIds, CancellationToken ct)
    {
        foreach (Guid groupId in groupIds.Distinct())
        {
            await cache.RemoveAsync(Key(groupId), ct);
        }
    }
}
