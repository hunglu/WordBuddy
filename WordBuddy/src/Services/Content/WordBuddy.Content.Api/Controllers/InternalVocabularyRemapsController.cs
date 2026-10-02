using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.VocabularyRemaps.Commands.AcknowledgeVocabularyWordIdRemaps;
using WordBuddy.Content.Application.Features.VocabularyRemaps.Queries.GetPendingVocabularyWordIdRemaps;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Controllers;

/// <summary>Service-to-service endpoints Progress uses to pull the word-id remaps recorded by the
/// <c>UnifyVocabularyWords</c> migration and acknowledge them once applied. Deliberately outside
/// <c>/api</c>: nginx, the Vite proxy and the k8s Ingress never route <c>/internal</c> to Content, so
/// it is reachable only on the container network. Requires a <c>wb_service=progress</c> token.</summary>
[ApiController]
[Route("internal/vocabulary-remaps")]
[Authorize(Policy = "InternalService")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class InternalVocabularyRemapsController : ControllerBase
{
    private readonly IQueryHandler<GetPendingVocabularyWordIdRemapsQuery, IReadOnlyList<VocabularyWordIdRemapDto>> _getPending;
    private readonly ICommandHandler<AcknowledgeVocabularyWordIdRemapsCommand> _acknowledge;
    private readonly ILogger<InternalVocabularyRemapsController> _logger;

    public InternalVocabularyRemapsController(
        IQueryHandler<GetPendingVocabularyWordIdRemapsQuery, IReadOnlyList<VocabularyWordIdRemapDto>> getPending,
        ICommandHandler<AcknowledgeVocabularyWordIdRemapsCommand> acknowledge,
        ILogger<InternalVocabularyRemapsController> logger)
    {
        _getPending = getPending;
        _acknowledge = acknowledge;
        _logger = logger;
    }

    /// <summary>Returns up to <paramref name="limit"/> (1–500) pending remaps, ordered by old id.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPending([FromQuery] int limit = GetPendingVocabularyWordIdRemapsQueryValidator.MaxLimit, CancellationToken ct = default)
    {
        Result<IReadOnlyList<VocabularyWordIdRemapDto>> result = await _getPending.HandleAsync(new GetPendingVocabularyWordIdRemapsQuery(limit), ct);
        return result.IsSuccess
            ? Ok(new PendingVocabularyWordIdRemapsResponse(result.Value))
            : result.ToProblemResult(this);
    }

    /// <summary>Marks the given old ids as applied by Progress. Idempotent.</summary>
    [HttpPost("acknowledge")]
    public async Task<IActionResult> Acknowledge([FromBody] AcknowledgeVocabularyWordIdRemapsRequest request, CancellationToken ct)
    {
        Result result = await _acknowledge.HandleAsync(new AcknowledgeVocabularyWordIdRemapsCommand(request.OldIds), ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("Acknowledge remaps succeeded: Count={Count}", request.OldIds.Count);
        return NoContent();
    }
}
