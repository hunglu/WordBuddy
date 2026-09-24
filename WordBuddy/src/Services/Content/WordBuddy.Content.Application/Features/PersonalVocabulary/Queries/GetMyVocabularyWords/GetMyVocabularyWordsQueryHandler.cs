using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetMyVocabularyWords;

public sealed class GetMyVocabularyWordsQueryHandler : IQueryHandler<GetMyVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>>
{
    private readonly IPersonalVocabularyWordRepository _repository;
    private readonly ILogger<GetMyVocabularyWordsQueryHandler> _logger;

    public GetMyVocabularyWordsQueryHandler(IPersonalVocabularyWordRepository repository, ILogger<GetMyVocabularyWordsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<PersonalVocabularyWordDto>>> HandleAsync(GetMyVocabularyWordsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetMyVocabularyWordsQuery started: OwnerUserId={OwnerUserId}", query.OwnerUserId);

        Result<IReadOnlyList<PersonalVocabularyWord>> wordsResult = await _repository.GetByOwnerAsync(query.OwnerUserId, ct);
        if (wordsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetMyVocabularyWordsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                wordsResult.Error.Code, wordsResult.Error.Description);
            return Result.Failure<IReadOnlyList<PersonalVocabularyWordDto>>(wordsResult.Error);
        }

        IReadOnlyList<PersonalVocabularyWordDto> dtos = wordsResult.Value.Select(ToDto).ToList();

        _logger.LogInformation("GetMyVocabularyWordsQuery succeeded: Count={Count}", dtos.Count);
        return Result.Success(dtos);
    }

    private static PersonalVocabularyWordDto ToDto(PersonalVocabularyWord word) => new(
        word.Id,
        word.OwnerUserId,
        word.Word,
        word.Definition,
        word.Example,
        word.ShareStatus,
        word.VisibleToChildren,
        word.CreatedAtUtc);
}
