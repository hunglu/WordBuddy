using Grpc.Core;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Shared.Kernel;
using WordBuddy.Shared.Protos.Content.V1;

namespace WordBuddy.Progress.Infrastructure.ContentClient;

/// <summary>
/// gRPC client for <c>ContentSenses.GetSensesByIds</c> on Content. Same contract as the REST
/// <see cref="ContentSenseClient"/>: forwards the caller's bearer token as <c>authorization</c> metadata
/// (see <see cref="ForwardBearerTokenHandler"/>), caches per session in <see cref="SessionSenseCache"/>,
/// and maps any transport failure to <c>Content.Unavailable</c>. Logs ids and counts only.
/// </summary>
internal sealed class ContentSenseGrpcClient : IContentSenseClient
{
    /// <summary>Content accepts 1–100 ids per call.</summary>
    private const int MaxIdsPerCall = 100;

    private readonly ContentSenses.ContentSensesClient _client;
    private readonly SessionSenseCache _cache;
    private readonly ILogger<ContentSenseGrpcClient> _logger;

    public ContentSenseGrpcClient(
        ContentSenses.ContentSensesClient client,
        SessionSenseCache cache,
        ILogger<ContentSenseGrpcClient> logger)
    {
        _client = client;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<ContentSenseDto>>> GetSessionSensesAsync(
        Guid sessionId,
        IReadOnlyCollection<Guid> senseIds,
        DateTime sessionExpiresAtUtc,
        CancellationToken ct = default)
    {
        List<ContentSenseDto>? hit = await _cache.GetAsync(sessionId, ct);
        if (hit is not null)
        {
            return Result.Success<IReadOnlyList<ContentSenseDto>>(hit);
        }

        List<ContentSenseDto> senses = [];
        try
        {
            foreach (Guid[] chunk in senseIds.Chunk(MaxIdsPerCall))
            {
                GetSensesByIdsRequest request = new();
                request.SenseIds.AddRange(chunk.Select(id => id.ToString()));

                GetSensesByIdsReply reply = await _client.GetSensesByIdsAsync(request, cancellationToken: ct);
                senses.AddRange(reply.Senses.Select(ToDto));
            }
        }
        catch (RpcException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Content gRPC senses call failed: SessionId={SessionId}, StatusCode={StatusCode}",
                sessionId, ex.StatusCode);
            return Unavailable();
        }
        catch (Exception ex) when ((ex is HttpRequestException or TaskCanceledException or FormatException
            or Polly.Timeout.TimeoutRejectedException or Polly.CircuitBreaker.BrokenCircuitException) && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Content gRPC senses call failed: SessionId={SessionId}", sessionId);
            return Unavailable();
        }

        await _cache.SetAsync(sessionId, senses, sessionExpiresAtUtc, ct);

        _logger.LogInformation(
            "Content senses loaded over gRPC: SessionId={SessionId}, Requested={Requested}, Returned={Returned}",
            sessionId, senseIds.Count, senses.Count);
        return Result.Success<IReadOnlyList<ContentSenseDto>>(senses);
    }

    private static ContentSenseDto ToDto(SenseReview sense) => new(
        Guid.Parse(sense.SenseId),
        sense.Word,
        sense.Definition,
        sense.HasImageUrl ? sense.ImageUrl : null,
        sense.HasAudioUrl ? sense.AudioUrl : null,
        sense.HasPersonalContext ? sense.PersonalContext : null);

    private static Result<IReadOnlyList<ContentSenseDto>> Unavailable() =>
        Result.Failure<IReadOnlyList<ContentSenseDto>>(
            Error.Failure("Content.Unavailable", "The word service is not available right now. Try again later."));
}
