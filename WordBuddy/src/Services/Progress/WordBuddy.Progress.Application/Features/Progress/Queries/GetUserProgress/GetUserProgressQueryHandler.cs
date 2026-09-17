using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Progress.Queries.GetUserProgress;

public sealed class GetUserProgressQueryHandler : IQueryHandler<GetUserProgressQuery, IReadOnlyList<LearnerProgressDto>>
{
    private readonly ILearnerProgressRepository _progressRepository;
    private readonly ILogger<GetUserProgressQueryHandler> _logger;

    public GetUserProgressQueryHandler(ILearnerProgressRepository progressRepository, ILogger<GetUserProgressQueryHandler> logger)
    {
        _progressRepository = progressRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<LearnerProgressDto>>> HandleAsync(GetUserProgressQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetUserProgressQuery started: UserId={UserId}", query.UserId);

        Result<IReadOnlyList<LearnerProgress>> progressResult = await _progressRepository.GetByUserAsync(query.UserId, ct);
        if (progressResult.IsFailure)
        {
            _logger.LogWarning(
                "GetUserProgressQuery repository failure: {ErrorCode} — {ErrorDescription}",
                progressResult.Error.Code, progressResult.Error.Description);
            return Result.Failure<IReadOnlyList<LearnerProgressDto>>(progressResult.Error);
        }

        IReadOnlyList<LearnerProgressDto> dtos = progressResult.Value
            .Select(p => new LearnerProgressDto(p.Id, p.LessonId, p.IsCompleted, p.ScorePercent, p.CompletedAtUtc))
            .ToList();

        _logger.LogInformation("GetUserProgressQuery succeeded: Count={Count}", dtos.Count);
        return Result.Success(dtos);
    }
}
