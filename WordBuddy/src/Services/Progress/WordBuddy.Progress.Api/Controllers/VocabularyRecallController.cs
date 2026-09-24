using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Progress.Api.Extensions;
using WordBuddy.Progress.Api.Models;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SubmitVocabularyRecallCheck;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Queries.GetVocabularyRecallProgress;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Api.Controllers;

/// <summary>Vocabulary recall-check submissions and progress — kept separate from
/// <see cref="ProgressController"/> to keep that controller focused on lesson completion.</summary>
[ApiController]
[Route("api/progress/vocabulary-recall")]
[Authorize]
public sealed class VocabularyRecallController : ControllerBase
{
    private readonly ICommandHandler<SubmitVocabularyRecallCheckCommand> _submitCheck;
    private readonly IQueryHandler<GetVocabularyRecallProgressQuery, VocabularyRecallProgressDto> _getProgress;
    private readonly ILogger<VocabularyRecallController> _logger;

    public VocabularyRecallController(
        ICommandHandler<SubmitVocabularyRecallCheckCommand> submitCheck,
        IQueryHandler<GetVocabularyRecallProgressQuery, VocabularyRecallProgressDto> getProgress,
        ILogger<VocabularyRecallController> logger)
    {
        _submitCheck = submitCheck;
        _getProgress = getProgress;
        _logger = logger;
    }

    /// <summary>Submits the authenticated user's recall-check results for a session.</summary>
    [HttpPost]
    public async Task<IActionResult> SubmitCheck([FromBody] SubmitVocabularyRecallCheckRequest request, CancellationToken ct)
    {
        Guid userId = User.GetUserId();

        Result result = await _submitCheck.HandleAsync(
            new SubmitVocabularyRecallCheckCommand(
                userId,
                request.Results
                    .Select(r => new VocabularyRecallResultItem(r.VocabularyWordId, r.Word, r.Known))
                    .ToList()),
            ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("SubmitCheck succeeded: UserId={UserId}, ResultCount={ResultCount}", userId, request.Results.Count);
        return NoContent();
    }

    /// <summary>Gets the authenticated user's recall-check progress — known/learning counts and recent sessions.</summary>
    [HttpGet]
    public async Task<IActionResult> GetProgress(CancellationToken ct)
    {
        Guid userId = User.GetUserId();

        Result<VocabularyRecallProgressDto> result = await _getProgress.HandleAsync(new GetVocabularyRecallProgressQuery(userId), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }
}
