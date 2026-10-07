using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddPersonalVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddSharedVocabularyWordToMyList;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.ModerateSharedVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RequestShareVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetMyVocabularyWords;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetPendingVocabularyModeration;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetRandomVocabularyWordsForCheck;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSensesByIds;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSharedVocabularyWords;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Controllers;

/// <summary>Personal vocabulary builder — a learner's own words, the moderated community sharing
/// pool, and random-word selection for a recall-check session.</summary>
[ApiController]
[Route("api/vocabulary")]
[Authorize]
public sealed class PersonalVocabularyController : ControllerBase
{
    private readonly ICommandHandler<AddPersonalVocabularyWordCommand, Guid> _addWord;
    private readonly ICommandHandler<RequestShareVocabularyWordCommand> _requestShare;
    private readonly ICommandHandler<ModerateSharedVocabularyWordCommand> _moderate;
    private readonly ICommandHandler<AddSharedVocabularyWordToMyListCommand, Guid> _addSharedToMyList;
    private readonly ICommandHandler<DeletePersonalVocabularyWordCommand> _deleteWord;
    private readonly IQueryHandler<GetMyVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>> _getMyWords;
    private readonly IQueryHandler<GetSharedVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>> _getSharedWords;
    private readonly IQueryHandler<GetRandomVocabularyWordsForCheckQuery, IReadOnlyList<PersonalVocabularyWordDto>> _getRandomForCheck;
    private readonly IQueryHandler<GetPendingVocabularyModerationQuery, IReadOnlyList<PersonalVocabularyWordDto>> _getPendingModeration;
    private readonly IQueryHandler<GetSensesByIdsQuery, IReadOnlyList<SenseReviewDto>> _getSensesByIds;
    private readonly ILogger<PersonalVocabularyController> _logger;

    public PersonalVocabularyController(
        ICommandHandler<AddPersonalVocabularyWordCommand, Guid> addWord,
        ICommandHandler<RequestShareVocabularyWordCommand> requestShare,
        ICommandHandler<ModerateSharedVocabularyWordCommand> moderate,
        ICommandHandler<AddSharedVocabularyWordToMyListCommand, Guid> addSharedToMyList,
        ICommandHandler<DeletePersonalVocabularyWordCommand> deleteWord,
        IQueryHandler<GetMyVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>> getMyWords,
        IQueryHandler<GetSharedVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>> getSharedWords,
        IQueryHandler<GetRandomVocabularyWordsForCheckQuery, IReadOnlyList<PersonalVocabularyWordDto>> getRandomForCheck,
        IQueryHandler<GetPendingVocabularyModerationQuery, IReadOnlyList<PersonalVocabularyWordDto>> getPendingModeration,
        IQueryHandler<GetSensesByIdsQuery, IReadOnlyList<SenseReviewDto>> getSensesByIds,
        ILogger<PersonalVocabularyController> logger)
    {
        _addWord = addWord;
        _requestShare = requestShare;
        _moderate = moderate;
        _addSharedToMyList = addSharedToMyList;
        _deleteWord = deleteWord;
        _getMyWords = getMyWords;
        _getSharedWords = getSharedWords;
        _getRandomForCheck = getRandomForCheck;
        _getPendingModeration = getPendingModeration;
        _getSensesByIds = getSensesByIds;
        _logger = logger;
    }

    /// <summary>Adds a word to the authenticated caller's own personal vocabulary list.</summary>
    [HttpPost]
    public async Task<IActionResult> AddWord([FromBody] AddPersonalVocabularyWordRequest request, CancellationToken ct)
    {
        Result<Guid> result = await _addWord.HandleAsync(
            new AddPersonalVocabularyWordCommand(User.GetUserId(), User.GetAgeGroup(), request.Word, request.Definition, request.Example), ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("AddWord succeeded: WordId={WordId}", result.Value);
        return CreatedAtAction(nameof(GetMyWords), new { }, result.Value);
    }

    /// <summary>Gets the authenticated caller's own personal vocabulary list, any share status.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyWords(CancellationToken ct)
    {
        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await _getMyWords.HandleAsync(new GetMyVocabularyWordsQuery(User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Submits the caller's own word for moderation review before it can be shared.
    /// Child accounts cannot initiate sharing.</summary>
    [Authorize(Policy = "CanShareVocabulary")]
    [HttpPost("{id:guid}/share")]
    public async Task<IActionResult> RequestShare(Guid id, CancellationToken ct)
    {
        Result result = await _requestShare.HandleAsync(new RequestShareVocabularyWordCommand(id, User.GetUserId()), ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("RequestShare succeeded: WordId={WordId}", id);
        return NoContent();
    }

    /// <summary>Deletes a word from the caller's own personal vocabulary list. An author deleting a
    /// <c>Shared</c> or <c>PendingReview</c> word must pass <c>?confirm=true</c>, otherwise 409.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteWord(Guid id, [FromQuery] bool confirm, CancellationToken ct)
    {
        Result result = await _deleteWord.HandleAsync(new DeletePersonalVocabularyWordCommand(id, User.GetUserId(), confirm), ct);
        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("DeleteWord succeeded: WordId={WordId}", id);
        return NoContent();
    }

    /// <summary>Browses the community sharing pool — Child callers only ever receive
    /// <c>VisibleToChildren</c> items, enforced at the query-handler level.</summary>
    [HttpGet("shared")]
    public async Task<IActionResult> GetSharedWords(CancellationToken ct)
    {
        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await _getSharedWords.HandleAsync(new GetSharedVocabularyWordsQuery(User.GetAgeGroup(), User.GetUserId()), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Adds a shared pool word to the caller's own list (a link, not a copy). Idempotent;
    /// returns the shared word's id.</summary>
    [HttpPost("shared/{id:guid}/add-to-mine")]
    public async Task<IActionResult> AddSharedWordToMyList(Guid id, CancellationToken ct)
    {
        Result<Guid> result = await _addSharedToMyList.HandleAsync(
            new AddSharedVocabularyWordToMyListCommand(id, User.GetUserId(), User.GetAgeGroup()), ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("AddSharedWordToMyList succeeded: NewWordId={NewWordId}", result.Value);
        return CreatedAtAction(nameof(GetMyWords), new { }, result.Value);
    }

    /// <summary>Starts a recall-check session — a random subset of the caller's own words.</summary>
    [HttpGet("check")]
    public async Task<IActionResult> GetRandomWordsForCheck([FromQuery] int count, CancellationToken ct)
    {
        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await _getRandomForCheck.HandleAsync(
            new GetRandomVocabularyWordsForCheckQuery(User.GetUserId(), count), ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Batch read of senses for a review session (<c>?ids=a&amp;ids=b</c>, 1–100 ids). Hidden
    /// and unknown ids are both omitted with the same 200; Child callers only get child-visible senses.</summary>
    [HttpGet("senses")]
    public async Task<IActionResult> GetSensesByIds([FromQuery] Guid[] ids, CancellationToken ct)
    {
        Result<IReadOnlyList<SenseReviewDto>> result = await _getSensesByIds.HandleAsync(
            new GetSensesByIdsQuery(ids, User.GetUserId(), User.GetAgeGroup()), ct);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Lists words awaiting moderation (admin only).</summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("moderation/pending")]
    public async Task<IActionResult> GetPendingModeration(CancellationToken ct)
    {
        Result<IReadOnlyList<PersonalVocabularyWordDto>> result = await _getPendingModeration.HandleAsync(new GetPendingVocabularyModerationQuery(), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Approves or rejects a pending-review word (admin only).</summary>
    [Authorize(Policy = "AdminOnly")]
    [HttpPost("moderation/{id:guid}")]
    public async Task<IActionResult> Moderate(Guid id, [FromBody] ModerateVocabularyWordRequest request, CancellationToken ct)
    {
        Result result = await _moderate.HandleAsync(
            new ModerateSharedVocabularyWordCommand(id, request.Approve, request.VisibleToChildren, User.GetUserId()), ct);

        if (result.IsFailure)
        {
            return result.ToProblemResult(this);
        }

        _logger.LogInformation("Moderate succeeded: WordId={WordId}, Approve={Approve}", id, request.Approve);
        return NoContent();
    }
}
