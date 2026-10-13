using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.SearchLessonSenses;

public sealed class SearchLessonSensesQueryHandler : IQueryHandler<SearchLessonSensesQuery, IReadOnlyList<AdminSenseSearchResultDto>>
{
    /// <summary>Most rows one search returns.</summary>
    public const int MaxResults = 50;

    private readonly IVocabularyWordRepository _senseRepository;
    private readonly IValidator<SearchLessonSensesQuery> _validator;
    private readonly ILogger<SearchLessonSensesQueryHandler> _logger;

    public SearchLessonSensesQueryHandler(
        IVocabularyWordRepository senseRepository,
        IValidator<SearchLessonSensesQuery> validator,
        ILogger<SearchLessonSensesQueryHandler> logger)
    {
        _senseRepository = senseRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AdminSenseSearchResultDto>>> HandleAsync(SearchLessonSensesQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("SearchLessonSensesQuery started");

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("SearchLessonSensesQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<IReadOnlyList<AdminSenseSearchResultDto>>(Error.Validation("SearchLessonSenses.Validation", validation.ToString()));
        }

        Result<IReadOnlyList<Sense>> sensesResult = await _senseRepository.SearchAttachableAsync(query.Search?.Trim(), MaxResults, ct);
        if (sensesResult.IsFailure)
        {
            _logger.LogWarning(
                "SearchLessonSensesQuery repository failure: {ErrorCode} — {ErrorDescription}",
                sensesResult.Error.Code, sensesResult.Error.Description);
            return Result.Failure<IReadOnlyList<AdminSenseSearchResultDto>>(sensesResult.Error);
        }

        IReadOnlyList<AdminSenseSearchResultDto> dtos = sensesResult.Value
            .Select(s => new AdminSenseSearchResultDto(
                s.Id, s.Word, s.Definition, s.Example, !s.IsHiddenFromChildrenInLessons, s.ShareStatus, s.Source))
            .ToList();

        _logger.LogInformation("SearchLessonSensesQuery succeeded: Count={Count}", dtos.Count);
        return Result.Success(dtos);
    }
}
