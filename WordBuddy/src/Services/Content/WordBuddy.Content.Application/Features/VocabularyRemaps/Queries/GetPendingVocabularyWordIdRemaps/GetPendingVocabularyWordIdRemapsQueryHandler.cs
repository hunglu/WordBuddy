using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.VocabularyRemaps.Queries.GetPendingVocabularyWordIdRemaps;

/// <summary>Returns pending word-id remaps ordered by old id. Deliberately not cached — the rows are
/// mutable (acknowledged by the only caller) and tiny.</summary>
public sealed class GetPendingVocabularyWordIdRemapsQueryHandler : IQueryHandler<GetPendingVocabularyWordIdRemapsQuery, IReadOnlyList<VocabularyWordIdRemapDto>>
{
    private readonly IVocabularyWordIdRemapRepository _repository;
    private readonly IValidator<GetPendingVocabularyWordIdRemapsQuery> _validator;
    private readonly ILogger<GetPendingVocabularyWordIdRemapsQueryHandler> _logger;

    public GetPendingVocabularyWordIdRemapsQueryHandler(
        IVocabularyWordIdRemapRepository repository,
        IValidator<GetPendingVocabularyWordIdRemapsQuery> validator,
        ILogger<GetPendingVocabularyWordIdRemapsQueryHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<VocabularyWordIdRemapDto>>> HandleAsync(GetPendingVocabularyWordIdRemapsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetPendingVocabularyWordIdRemapsQuery started: Limit={Limit}", query.Limit);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetPendingVocabularyWordIdRemapsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<IReadOnlyList<VocabularyWordIdRemapDto>>(
                Error.Validation("GetPendingVocabularyWordIdRemaps.Validation", validation.ToString()));
        }

        Result<IReadOnlyList<VocabularyWordIdRemapDto>> result = await _repository.GetPendingAsync(query.Limit, ct);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "GetPendingVocabularyWordIdRemapsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                result.Error.Code, result.Error.Description);
            return result;
        }

        _logger.LogInformation("GetPendingVocabularyWordIdRemapsQuery succeeded: Count={Count}", result.Value.Count);
        return result;
    }
}
