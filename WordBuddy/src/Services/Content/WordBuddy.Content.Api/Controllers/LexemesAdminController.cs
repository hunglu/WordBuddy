using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Content.Api.Extensions;
using WordBuddy.Content.Api.Models;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.LexemesAdmin.Commands.RemoveLexemeAudio;
using WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UpdateLexeme;
using WordBuddy.Content.Application.Features.LexemesAdmin.Commands.UploadLexemeAudio;
using WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemeDetail;
using WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemes;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Api.Controllers;

/// <summary>Admin-only dictionary data: search lexemes, correct their fields, and set or remove the
/// UK/US pronunciation audio.</summary>
[ApiController]
[Route("api/vocabulary/admin/lexemes")]
[Authorize(Policy = "AdminOnly")]
public sealed class LexemesAdminController : ControllerBase
{
    // 5 MB file plus multipart overhead.
    private const long MaxRequestBytes = UploadLexemeAudioCommand.MaxBytes + 1024 * 1024;

    private readonly IQueryHandler<GetAdminLexemesQuery, PagedResult<AdminLexemeListItemDto>> _getLexemes;
    private readonly IQueryHandler<GetAdminLexemeDetailQuery, AdminLexemeDetailDto> _getLexemeDetail;
    private readonly ICommandHandler<UpdateLexemeCommand> _updateLexeme;
    private readonly ICommandHandler<UploadLexemeAudioCommand, MediaAssetDto> _uploadAudio;
    private readonly ICommandHandler<RemoveLexemeAudioCommand> _removeAudio;

    public LexemesAdminController(
        IQueryHandler<GetAdminLexemesQuery, PagedResult<AdminLexemeListItemDto>> getLexemes,
        IQueryHandler<GetAdminLexemeDetailQuery, AdminLexemeDetailDto> getLexemeDetail,
        ICommandHandler<UpdateLexemeCommand> updateLexeme,
        ICommandHandler<UploadLexemeAudioCommand, MediaAssetDto> uploadAudio,
        ICommandHandler<RemoveLexemeAudioCommand> removeAudio)
    {
        _getLexemes = getLexemes;
        _getLexemeDetail = getLexemeDetail;
        _updateLexeme = updateLexeme;
        _uploadAudio = uploadAudio;
        _removeAudio = removeAudio;
    }

    /// <summary>Pages lexemes. <c>search</c> matches any part of the word; matches that start with it come first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLexemes(
        [FromQuery] string? search,
        [FromQuery] PartOfSpeech? partOfSpeech,
        [FromQuery] CefrLevel? cefrLevel,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paging.DefaultPageSize,
        CancellationToken ct = default)
    {
        Result<PagedResult<AdminLexemeListItemDto>> result = await _getLexemes.HandleAsync(
            new GetAdminLexemesQuery(search, partOfSpeech, cefrLevel, page, pageSize), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Gets one lexeme with its audio and its catalog senses. Private learner senses are only counted.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLexeme(Guid id, CancellationToken ct)
    {
        Result<AdminLexemeDetailDto> result = await _getLexemeDetail.HandleAsync(new GetAdminLexemeDetailQuery(id), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Corrects the dictionary fields of a lexeme. The word itself does not change.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateLexeme(Guid id, [FromBody] UpdateLexemeRequest request, CancellationToken ct)
    {
        if (request.PartOfSpeech is null)
        {
            return Result.Failure(Error.Validation("UpdateLexeme.Validation", "The part of speech is required."))
                .ToProblemResult(this);
        }

        Result result = await _updateLexeme.HandleAsync(
            new UpdateLexemeCommand(
                id,
                request.PartOfSpeech.Value,
                request.IpaUk,
                request.IpaUs,
                request.Syllables,
                request.WordForms,
                request.CefrLevel,
                request.FrequencyRank),
            ct);
        return NoContentOrProblem(result);
    }

    /// <summary>Sets the pronunciation of a lexeme from one MP3 file (max 5 MB). <c>locale</c> is en-GB or en-US.</summary>
    [HttpPut("{id:guid}/audio/{locale}")]
    [RequestSizeLimit(MaxRequestBytes)]
    public async Task<IActionResult> UploadAudio(Guid id, string locale, IFormFile file, CancellationToken ct)
    {
        await using Stream stream = file.OpenReadStream();
        Result<MediaAssetDto> result = await _uploadAudio.HandleAsync(
            new UploadLexemeAudioCommand(id, locale, stream, file.ContentType, file.Length), ct);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemResult(this);
    }

    /// <summary>Removes a pronunciation. The file is deleted unless something else still uses it.</summary>
    [HttpDelete("{id:guid}/audio/{locale}")]
    public async Task<IActionResult> RemoveAudio(Guid id, string locale, CancellationToken ct) =>
        NoContentOrProblem(await _removeAudio.HandleAsync(new RemoveLexemeAudioCommand(id, locale), ct));

    private IActionResult NoContentOrProblem(Result result) => result.IsSuccess ? NoContent() : result.ToProblemResult(this);
}
