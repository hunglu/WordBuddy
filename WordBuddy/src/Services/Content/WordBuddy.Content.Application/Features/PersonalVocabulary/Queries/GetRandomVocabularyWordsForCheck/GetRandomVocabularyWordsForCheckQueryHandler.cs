using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetRandomVocabularyWordsForCheck;

public sealed class GetRandomVocabularyWordsForCheckQueryHandler : IQueryHandler<GetRandomVocabularyWordsForCheckQuery, IReadOnlyList<PersonalVocabularyWordDto>>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<GetRandomVocabularyWordsForCheckQuery> _validator;
    private readonly ILogger<GetRandomVocabularyWordsForCheckQueryHandler> _logger;

    public GetRandomVocabularyWordsForCheckQueryHandler(
        IVocabularyWordRepository repository,
        IValidator<GetRandomVocabularyWordsForCheckQuery> validator,
        ILogger<GetRandomVocabularyWordsForCheckQueryHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PersonalVocabularyWordDto>>> HandleAsync(GetRandomVocabularyWordsForCheckQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetRandomVocabularyWordsForCheckQuery started: OwnerUserId={OwnerUserId}, Count={Count}",
            query.OwnerUserId, query.Count);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetRandomVocabularyWordsForCheckQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<IReadOnlyList<PersonalVocabularyWordDto>>(
                Error.Validation("GetRandomVocabularyWordsForCheck.Validation", validation.ToString()));
        }

        Result<IReadOnlyList<LearnerWord>> wordsResult = await _repository.GetRandomLinkedToUserAsync(query.OwnerUserId, query.Count, ct);
        if (wordsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetRandomVocabularyWordsForCheckQuery repository failure: {ErrorCode} — {ErrorDescription}",
                wordsResult.Error.Code, wordsResult.Error.Description);
            return Result.Failure<IReadOnlyList<PersonalVocabularyWordDto>>(wordsResult.Error);
        }

        IReadOnlyList<PersonalVocabularyWordDto> dtos = wordsResult.Value.Select(PersonalVocabularyWordMapper.ToDto).ToList();

        _logger.LogInformation("GetRandomVocabularyWordsForCheckQuery succeeded: Count={Count}", dtos.Count);
        return Result.Success(dtos);
    }
}
