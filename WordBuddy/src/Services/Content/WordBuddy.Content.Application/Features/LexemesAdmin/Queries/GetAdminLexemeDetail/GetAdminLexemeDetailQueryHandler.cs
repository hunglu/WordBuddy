using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemeDetail;

public sealed class GetAdminLexemeDetailQueryHandler : IQueryHandler<GetAdminLexemeDetailQuery, AdminLexemeDetailDto>
{
    private readonly ILexemeAdminRepository _repository;
    private readonly IValidator<GetAdminLexemeDetailQuery> _validator;
    private readonly ILogger<GetAdminLexemeDetailQueryHandler> _logger;

    public GetAdminLexemeDetailQueryHandler(
        ILexemeAdminRepository repository,
        IValidator<GetAdminLexemeDetailQuery> validator,
        ILogger<GetAdminLexemeDetailQueryHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AdminLexemeDetailDto>> HandleAsync(GetAdminLexemeDetailQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetAdminLexemeDetailQuery started: LexemeId={LexemeId}", query.LexemeId);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetAdminLexemeDetailQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<AdminLexemeDetailDto>(Error.Validation("GetAdminLexemeDetail.Validation", validation.ToString()));
        }

        Result<LexemeDetailData> result = await _repository.GetDetailAsync(query.LexemeId, ct);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "GetAdminLexemeDetailQuery repository failure: {ErrorCode} — {ErrorDescription}",
                result.Error.Code, result.Error.Description);
            return Result.Failure<AdminLexemeDetailDto>(result.Error);
        }

        Lexeme lexeme = result.Value.Lexeme;
        IReadOnlyList<Sense> senses = result.Value.CatalogSenses;

        // ADR 0004: take the lemma from a catalog sense; never show a learner's own spelling.
        string lemma = lexeme.RederiveLemma(senses.Select(s => s.Word)).IsSuccess
            ? lexeme.Lemma
            : lexeme.NormalizedLemma.ToLowerInvariant();

        AdminLexemeDetailDto dto = new(
            lexeme.Id,
            lemma,
            lexeme.PartOfSpeech,
            lexeme.IpaUk,
            lexeme.IpaUs,
            lexeme.CefrLevel,
            lexeme.FrequencyRank,
            lexeme.UkAudioAssetId is not null,
            lexeme.UsAudioAssetId is not null,
            senses.Count,
            result.Value.PrivateLearnerSenseCount,
            lexeme.Syllables,
            lexeme.WordForms,
            lexeme.UkAudio?.Url,
            lexeme.UsAudio?.Url,
            lexeme.UkAudioAssetId,
            lexeme.UsAudioAssetId,
            lexeme.CreatedAtUtc,
            senses
                .Select(s => new AdminLexemeSenseDto(
                    s.Id, s.Word, s.Definition, s.Example, s.Source, s.ShareStatus, s.Origin, s.VisibleToChildren, s.CreatedAtUtc))
                .ToList());

        _logger.LogInformation(
            "GetAdminLexemeDetailQuery succeeded: LexemeId={LexemeId}, CatalogSenses={CatalogSenses}, PrivateSenses={PrivateSenses}",
            lexeme.Id, dto.CatalogSenseCount, dto.PrivateLearnerSenseCount);
        return Result.Success(dto);
    }
}
