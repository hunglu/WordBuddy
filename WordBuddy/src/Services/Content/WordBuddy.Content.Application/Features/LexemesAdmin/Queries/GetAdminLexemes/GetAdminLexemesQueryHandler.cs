using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemes;

public sealed class GetAdminLexemesQueryHandler : IQueryHandler<GetAdminLexemesQuery, AdminLexemePageDto>
{
    private readonly ILexemeAdminRepository _repository;
    private readonly IValidator<GetAdminLexemesQuery> _validator;
    private readonly ILogger<GetAdminLexemesQueryHandler> _logger;

    public GetAdminLexemesQueryHandler(
        ILexemeAdminRepository repository,
        IValidator<GetAdminLexemesQuery> validator,
        ILogger<GetAdminLexemesQueryHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<AdminLexemePageDto>> HandleAsync(GetAdminLexemesQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetAdminLexemesQuery started: HasSearch={HasSearch}, PartOfSpeech={PartOfSpeech}, CefrLevel={CefrLevel}, Page={Page}, PageSize={PageSize}",
            !string.IsNullOrWhiteSpace(query.Search), query.PartOfSpeech, query.CefrLevel, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetAdminLexemesQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<AdminLexemePageDto>(Error.Validation("GetAdminLexemes.Validation", validation.ToString()));
        }

        // Same normalization as the NormalizedLemma column: trim spaces + upper-case.
        string? term = string.IsNullOrWhiteSpace(query.Search) ? null : Sense.NormalizeWord(query.Search);

        Result<AdminLexemePageDto> result = await _repository.SearchAsync(
            term, query.PartOfSpeech, query.CefrLevel, query.Page, query.PageSize, ct);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "GetAdminLexemesQuery repository failure: {ErrorCode} — {ErrorDescription}",
                result.Error.Code, result.Error.Description);
            return result;
        }

        _logger.LogInformation("GetAdminLexemesQuery succeeded: Count={Count}, TotalCount={TotalCount}", result.Value.Items.Count, result.Value.TotalCount);
        return result;
    }
}
