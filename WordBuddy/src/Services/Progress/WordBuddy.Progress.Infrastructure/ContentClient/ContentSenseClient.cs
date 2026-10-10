using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.ContentClient;

/// <summary>
/// Typed HTTP client for <c>GET /api/vocabulary/senses?ids=</c> on Content. Forwards the caller's
/// bearer token (see <see cref="ForwardBearerTokenHandler"/>). The reply is cached per session in
/// <see cref="IDistributedCache"/> under <c>progress:session-senses:{sessionId}</c> until the session
/// expires, so one Content call serves the whole session. Logs ids and counts only.
/// </summary>
internal sealed class ContentSenseClient : IContentSenseClient
{
    /// <summary>Content accepts 1–100 ids per call.</summary>
    private const int MaxIdsPerCall = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ContentSenseClient> _logger;

    public ContentSenseClient(
        HttpClient httpClient,
        IDistributedCache cache,
        TimeProvider timeProvider,
        ILogger<ContentSenseClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>Cache key for a session's senses: <c>progress:session-senses:{sessionId}</c>.</summary>
    public static string CacheKey(Guid sessionId) => $"progress:session-senses:{sessionId}";

    public async Task<Result<IReadOnlyList<ContentSenseDto>>> GetSessionSensesAsync(
        Guid sessionId,
        IReadOnlyCollection<Guid> senseIds,
        DateTime sessionExpiresAtUtc,
        CancellationToken ct = default)
    {
        string key = CacheKey(sessionId);
        byte[]? cached = await _cache.GetAsync(key, ct);
        if (cached is not null)
        {
            List<ContentSenseDto>? hit = JsonSerializer.Deserialize<List<ContentSenseDto>>(cached, JsonOptions);
            if (hit is not null)
            {
                return Result.Success<IReadOnlyList<ContentSenseDto>>(hit);
            }
        }

        List<ContentSenseDto> senses = [];
        try
        {
            foreach (Guid[] chunk in senseIds.Chunk(MaxIdsPerCall))
            {
                string query = string.Join('&', chunk.Select(id => $"ids={id}"));
                using HttpResponseMessage response = await _httpClient.GetAsync($"api/vocabulary/senses?{query}", ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Content senses call failed: SessionId={SessionId}, StatusCode={StatusCode}",
                        sessionId, (int)response.StatusCode);
                    return Unavailable();
                }

                List<ContentSenseDto>? part = await response.Content.ReadFromJsonAsync<List<ContentSenseDto>>(JsonOptions, ct);
                if (part is not null)
                {
                    senses.AddRange(part);
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Content senses call failed: SessionId={SessionId}", sessionId);
            return Unavailable();
        }

        DateTimeOffset expiresAt = new(DateTime.SpecifyKind(sessionExpiresAtUtc, DateTimeKind.Utc));
        if (expiresAt > _timeProvider.GetUtcNow())
        {
            await _cache.SetAsync(
                key,
                JsonSerializer.SerializeToUtf8Bytes(senses, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpiration = expiresAt },
                ct);
        }

        _logger.LogInformation(
            "Content senses loaded: SessionId={SessionId}, Requested={Requested}, Returned={Returned}",
            sessionId, senseIds.Count, senses.Count);
        return Result.Success<IReadOnlyList<ContentSenseDto>>(senses);
    }

    private static Result<IReadOnlyList<ContentSenseDto>> Unavailable() =>
        Result.Failure<IReadOnlyList<ContentSenseDto>>(
            Error.Failure("Content.Unavailable", "The word service is not available right now. Try again later."));
}
