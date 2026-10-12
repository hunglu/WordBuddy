using Microsoft.Extensions.Caching.Distributed;

namespace WordBuddy.Progress.Application.Features.Challenges;

/// <summary>Cache keys of the challenge board: <c>progress:challenge-board:{id}</c>, absolute expiry 5 minutes.</summary>
public static class ChallengeCache
{
    /// <summary>Cache lifetime of a board snapshot.</summary>
    public static readonly TimeSpan BoardLifetime = TimeSpan.FromMinutes(5);

    /// <summary>The key of one board snapshot.</summary>
    public static string BoardKey(Guid challengeId) => $"progress:challenge-board:{challengeId}";

    /// <summary>Deletes the board snapshots of the given challenges (call after a successful write).</summary>
    public static async Task InvalidateBoardsAsync(IDistributedCache cache, IEnumerable<Guid> challengeIds, CancellationToken ct = default)
    {
        foreach (Guid id in challengeIds.Distinct())
        {
            await cache.RemoveAsync(BoardKey(id), ct);
        }
    }
}
