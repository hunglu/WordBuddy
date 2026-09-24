using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Queries.GetVocabularyRecallProgress;

public sealed class GetVocabularyRecallProgressQueryHandler : IQueryHandler<GetVocabularyRecallProgressQuery, VocabularyRecallProgressDto>
{
    private const int RecentSessionsTake = 10;

    private readonly IVocabularyRecallRepository _repository;
    private readonly ILogger<GetVocabularyRecallProgressQueryHandler> _logger;

    public GetVocabularyRecallProgressQueryHandler(IVocabularyRecallRepository repository, ILogger<GetVocabularyRecallProgressQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<VocabularyRecallProgressDto>> HandleAsync(GetVocabularyRecallProgressQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetVocabularyRecallProgressQuery started: UserId={UserId}", query.UserId);

        Result<IReadOnlyList<VocabularyRecallStat>> statsResult = await _repository.GetStatsByUserAsync(query.UserId, ct);
        if (statsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetVocabularyRecallProgressQuery repository failure (stats): {ErrorCode} — {ErrorDescription}",
                statsResult.Error.Code, statsResult.Error.Description);
            return Result.Failure<VocabularyRecallProgressDto>(statsResult.Error);
        }

        Result<IReadOnlyList<VocabularyRecallSession>> sessionsResult = await _repository.GetRecentSessionsByUserAsync(query.UserId, RecentSessionsTake, ct);
        if (sessionsResult.IsFailure)
        {
            _logger.LogWarning(
                "GetVocabularyRecallProgressQuery repository failure (sessions): {ErrorCode} — {ErrorDescription}",
                sessionsResult.Error.Code, sessionsResult.Error.Description);
            return Result.Failure<VocabularyRecallProgressDto>(sessionsResult.Error);
        }

        int knownCount = statsResult.Value.Count(s => s.Status == RecallStatus.Known);
        int learningCount = statsResult.Value.Count - knownCount;

        VocabularyRecallProgressDto dto = new(
            statsResult.Value.Count,
            knownCount,
            learningCount,
            sessionsResult.Value
                .Select(s => new VocabularyRecallSessionDto(s.Id, s.CheckedAtUtc, s.WordsChecked, s.WordsKnown))
                .ToList());

        _logger.LogInformation(
            "GetVocabularyRecallProgressQuery succeeded: UserId={UserId}, TotalWordsTracked={TotalWordsTracked}",
            query.UserId, dto.TotalWordsTracked);
        return Result.Success(dto);
    }
}
