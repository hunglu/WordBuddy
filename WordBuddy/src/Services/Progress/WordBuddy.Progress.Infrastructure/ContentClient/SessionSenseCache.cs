using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Infrastructure.ContentClient;

/// <summary>
/// Per-session cache of Content senses, shared by the REST and the gRPC client so both behave the same.
/// Key <c>progress:session-senses:{sessionId}</c>, absolute expiry at the session's end.
/// </summary>
internal sealed class SessionSenseCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDistributedCache _cache;
    private readonly TimeProvider _timeProvider;

    public SessionSenseCache(IDistributedCache cache, TimeProvider timeProvider)
    {
        _cache = cache;
        _timeProvider = timeProvider;
    }

    /// <summary>Cache key for a session's senses: <c>progress:session-senses:{sessionId}</c>.</summary>
    public static string CacheKey(Guid sessionId) => $"progress:session-senses:{sessionId}";

    /// <summary>The cached senses, or <see langword="null"/> on a miss.</summary>
    public async Task<List<ContentSenseDto>?> GetAsync(Guid sessionId, CancellationToken ct)
    {
        byte[]? cached = await _cache.GetAsync(CacheKey(sessionId), ct);
        return cached is null ? null : JsonSerializer.Deserialize<List<ContentSenseDto>>(cached, JsonOptions);
    }

    /// <summary>Stores the senses until <paramref name="sessionExpiresAtUtc"/>. An expiry in the past is not cached.</summary>
    public async Task SetAsync(Guid sessionId, IReadOnlyList<ContentSenseDto> senses, DateTime sessionExpiresAtUtc, CancellationToken ct)
    {
        DateTimeOffset expiresAt = new(DateTime.SpecifyKind(sessionExpiresAtUtc, DateTimeKind.Utc));
        if (expiresAt <= _timeProvider.GetUtcNow())
        {
            return;
        }

        await _cache.SetAsync(
            CacheKey(sessionId),
            JsonSerializer.SerializeToUtf8Bytes(senses, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt },
            ct);
    }
}
