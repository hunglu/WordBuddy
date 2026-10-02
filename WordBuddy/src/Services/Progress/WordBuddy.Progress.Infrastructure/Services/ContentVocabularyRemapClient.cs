using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Services;

/// <summary>Typed <see cref="HttpClient"/> for Content's <c>/internal/vocabulary-remaps</c> endpoints.
/// Maps every expected failure to a <see cref="ContentApiErrors"/> result: 401/403 →
/// <c>ContentApi.Unauthorized</c>; 404 (Content not migrated yet), other non-success codes, network
/// errors and timeouts → <c>ContentApi.Unavailable</c>. Retries live in the resilience handler.</summary>
public sealed class ContentVocabularyRemapClient : IContentVocabularyRemapClient
{
    /// <summary>Name of the underlying <see cref="IHttpClientFactory"/> client.</summary>
    public const string HttpClientName = "ContentVocabularyRemapClient";

    private const string PendingPath = "internal/vocabulary-remaps";
    private const string AcknowledgePath = "internal/vocabulary-remaps/acknowledge";

    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentVocabularyRemapClient> _logger;

    public ContentVocabularyRemapClient(HttpClient httpClient, ILogger<ContentVocabularyRemapClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<VocabularyWordIdRemapPair>>> GetPendingAsync(int limit, CancellationToken ct = default)
    {
        _logger.LogDebug("Fetching pending vocabulary remaps from Content: Limit={Limit}", limit);

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync($"{PendingPath}?limit={limit}", ct);
            Error? error = MapFailure(response.StatusCode);
            if (error is not null)
            {
                return Result.Failure<IReadOnlyList<VocabularyWordIdRemapPair>>(error);
            }

            PendingRemapsResponse? body = await response.Content.ReadFromJsonAsync<PendingRemapsResponse>(ct);
            if (body?.Items is null)
            {
                return Result.Failure<IReadOnlyList<VocabularyWordIdRemapPair>>(
                    ContentApiErrors.Unavailable("Content returned an empty or malformed remap response."));
            }

            return Result.Success<IReadOnlyList<VocabularyWordIdRemapPair>>(body.Items);
        }
        catch (Exception ex) when (IsTransportFailure(ex, ct))
        {
            return Result.Failure<IReadOnlyList<VocabularyWordIdRemapPair>>(
                ContentApiErrors.Unavailable($"Content could not be reached: {ex.GetType().Name}."));
        }
    }

    public async Task<Result> AcknowledgeAsync(IReadOnlyList<Guid> oldIds, CancellationToken ct = default)
    {
        _logger.LogDebug("Acknowledging vocabulary remaps to Content: Count={Count}", oldIds.Count);

        try
        {
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(AcknowledgePath, new AcknowledgeRequest(oldIds), ct);
            Error? error = MapFailure(response.StatusCode);
            return error is null ? Result.Success() : Result.Failure(error);
        }
        catch (Exception ex) when (IsTransportFailure(ex, ct))
        {
            return Result.Failure(ContentApiErrors.Unavailable($"Content could not be reached: {ex.GetType().Name}."));
        }
    }

    private static Error? MapFailure(HttpStatusCode statusCode)
    {
        int code = (int)statusCode;
        if (code is >= 200 and < 300)
        {
            return null;
        }

        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                ContentApiErrors.Unauthorized($"Content rejected the service token (HTTP {code})."),
            HttpStatusCode.NotFound =>
                ContentApiErrors.Unavailable("Content has no remap endpoint yet (HTTP 404) — not migrated or not deployed."),
            _ => ContentApiErrors.Unavailable($"Content returned HTTP {code}."),
        };
    }

    /// <summary>Network errors, resilience rejections (timeouts, open circuit) and HTTP timeouts — but
    /// not a cancellation the caller asked for, which propagates.</summary>
    private static bool IsTransportFailure(Exception ex, CancellationToken ct) => ex switch
    {
        HttpRequestException => true,
        ExecutionRejectedException => true,
        OperationCanceledException => !ct.IsCancellationRequested,
        System.Text.Json.JsonException => true,
        _ => false,
    };

    private sealed record PendingRemapsResponse(IReadOnlyList<VocabularyWordIdRemapPair>? Items);

    private sealed record AcknowledgeRequest(IReadOnlyList<Guid> OldIds);
}
