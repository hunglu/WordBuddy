using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Queries.GetLearnerWordStates;

/// <summary>Returns the caller's active states only. Logs ids and counts only.</summary>
public sealed class GetLearnerWordStatesQueryHandler : IQueryHandler<GetLearnerWordStatesQuery, IReadOnlyList<LearnerWordStateDto>>
{
    private readonly ILearnerWordStateRepository _states;
    private readonly ILogger<GetLearnerWordStatesQueryHandler> _logger;

    public GetLearnerWordStatesQueryHandler(ILearnerWordStateRepository states, ILogger<GetLearnerWordStatesQueryHandler> logger)
    {
        _states = states;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<LearnerWordStateDto>>> HandleAsync(GetLearnerWordStatesQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetLearnerWordStatesQuery started: UserId={UserId}", query.UserId);

        Result<IReadOnlyList<LearnerWordState>> states = await _states.GetActiveAsync(query.UserId, ct);
        if (states.IsFailure)
        {
            return Result.Failure<IReadOnlyList<LearnerWordStateDto>>(states.Error);
        }

        IReadOnlyList<LearnerWordStateDto> dtos = states.Value
            .Select(s => new LearnerWordStateDto(s.SenseId, s.Status, s.DueAtUtc, s.Reps, s.Lapses))
            .ToList();

        _logger.LogInformation("GetLearnerWordStatesQuery succeeded: UserId={UserId}, Count={Count}", query.UserId, dtos.Count);
        return Result.Success(dtos);
    }
}
