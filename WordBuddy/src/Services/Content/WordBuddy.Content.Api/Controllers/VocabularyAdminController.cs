using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RepublishLearnerWords;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Controllers;

/// <summary>Admin-only vocabulary maintenance operations.</summary>
[ApiController]
[Route("api/vocabulary/admin")]
[Authorize(Policy = "AdminOnly")]
public sealed class VocabularyAdminController : ControllerBase
{
    private readonly ICommandHandler<RepublishLearnerWordsCommand, RepublishLearnerWordsResult> _republish;
    private readonly ILogger<VocabularyAdminController> _logger;

    public VocabularyAdminController(
        ICommandHandler<RepublishLearnerWordsCommand, RepublishLearnerWordsResult> republish,
        ILogger<VocabularyAdminController> logger)
    {
        _republish = republish;
        _logger = logger;
    }

    /// <summary>Republishes <c>LearnerWordAdded</c> for every learner link (one-time backfill for
    /// Progress learning states, WB-22). Safe to run twice. Returns <c>{ published }</c>.</summary>
    [HttpPost("learner-words/republish")]
    public async Task<IActionResult> RepublishLearnerWords(CancellationToken ct)
    {
        Result<RepublishLearnerWordsResult> result = await _republish.HandleAsync(new RepublishLearnerWordsCommand(), ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("RepublishLearnerWords succeeded: Published={Published}", result.Value.Published);
        return Ok(result.Value);
    }
}
