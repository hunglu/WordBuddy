using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetMyVocabularyWords;

public sealed class GetMyVocabularyWordsQueryHandler : IQueryHandler<GetMyVocabularyWordsQuery, PagedResult<PersonalVocabularyWordDto>>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<GetMyVocabularyWordsQuery> _validator;
    private readonly ILogger<GetMyVocabularyWordsQueryHandler> _logger;

    public GetMyVocabularyWordsQueryHandler(
        IVocabularyWordRepository repository,
        IValidator<GetMyVocabularyWordsQuery> validator,
        ILogger<GetMyVocabularyWordsQueryHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<PersonalVocabularyWordDto>>> HandleAsync(GetMyVocabularyWordsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetMyVocabularyWordsQuery started: OwnerUserId={OwnerUserId}, Page={Page}, PageSize={PageSize}",
            query.OwnerUserId, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetMyVocabularyWordsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<PersonalVocabularyWordDto>>(Error.Validation("GetMyVocabularyWords.Validation", validation.ToString()));
        }

        Result<PagedResult<LearnerWord>> wordsResult = await _repository.GetLinkedToUserAsync(query.OwnerUserId, query.Page, query.PageSize, ct);
        if (wordsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetMyVocabularyWordsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                wordsResult.Error.Code, wordsResult.Error.Description);
            return Result.Failure<PagedResult<PersonalVocabularyWordDto>>(wordsResult.Error);
        }

        PagedResult<LearnerWord> words = wordsResult.Value;
        List<PersonalVocabularyWordDto> dtos = words.Items
            .Select(link => PersonalVocabularyWordMapper.ToDto(link, query.RequestingAgeGroup))
            .ToList();

        _logger.LogInformation("GetMyVocabularyWordsQuery succeeded: Count={Count}, TotalCount={TotalCount}", dtos.Count, words.TotalCount);
        return Result.Success(new PagedResult<PersonalVocabularyWordDto>(dtos, words.Page, words.PageSize, words.TotalCount));
    }
}
