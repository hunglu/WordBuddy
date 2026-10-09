using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSensesByIds;

/// <summary>Returns the visible senses among the requested ids. Logs counts only, never word text.</summary>
public sealed class GetSensesByIdsQueryHandler : IQueryHandler<GetSensesByIdsQuery, IReadOnlyList<SenseReviewDto>>
{
    private readonly IVocabularyWordRepository _repository;
    private readonly IValidator<GetSensesByIdsQuery> _validator;
    private readonly ILogger<GetSensesByIdsQueryHandler> _logger;

    public GetSensesByIdsQueryHandler(
        IVocabularyWordRepository repository,
        IValidator<GetSensesByIdsQuery> validator,
        ILogger<GetSensesByIdsQueryHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SenseReviewDto>>> HandleAsync(GetSensesByIdsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetSensesByIdsQuery started: RequestedCount={RequestedCount}, AgeGroup={AgeGroup}",
            query.Ids?.Count ?? 0, query.RequestingAgeGroup);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetSensesByIdsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<IReadOnlyList<SenseReviewDto>>(
                Error.Validation("GetSensesByIds.Validation", validation.ToString()));
        }

        List<Guid> ids = query.Ids.Distinct().ToList();
        Result<IReadOnlyList<SenseReviewCandidate>> candidatesResult =
            await _repository.GetForReviewAsync(query.RequestingUserId, ids, ct);
        if (candidatesResult.IsFailure)
        {
            _logger.LogWarning(
                "GetSensesByIdsQuery repository failure: {ErrorCode} — {ErrorDescription}",
                candidatesResult.Error.Code, candidatesResult.Error.Description);
            return Result.Failure<IReadOnlyList<SenseReviewDto>>(candidatesResult.Error);
        }

        IReadOnlyList<SenseReviewDto> dtos = candidatesResult.Value
            .Where(c => c.Sense.IsVisibleTo(query.RequestingUserId, query.RequestingAgeGroup, c.CallerLink))
            .Select(c => ToDto(c, query.RequestingUserId))
            .ToList();

        _logger.LogInformation(
            "GetSensesByIdsQuery succeeded: RequestedCount={RequestedCount}, ReturnedCount={ReturnedCount}",
            ids.Count, dtos.Count);
        return Result.Success(dtos);
    }

    private static SenseReviewDto ToDto(SenseReviewCandidate candidate, Guid userId)
    {
        Sense sense = candidate.Sense;
        string? personalContext = candidate.CallerLink is { } link && link.UserId == userId && link.SenseId == sense.Id
            ? link.PersonalContext
            : null;

        return new SenseReviewDto(
            sense.Id,
            sense.Word,
            sense.Definition,
            sense.Example,
            sense.Audio?.Url,
            sense.Image?.Url,
            personalContext);
    }
}
