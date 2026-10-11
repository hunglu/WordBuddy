using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.RecomputeChallengeScores;

/// <summary>The score projection. Idempotent: scores and dense ranks depend only on the review logs and the profiles.</summary>
public sealed class RecomputeChallengeScoresCommandHandler : ICommandHandler<RecomputeChallengeScoresCommand, int>
{
    private readonly IChallengeRepository _challenges;
    private readonly ChallengeScoreProjector _projector;
    private readonly IDistributedCache _cache;
    private readonly ILogger<RecomputeChallengeScoresCommandHandler> _logger;

    public RecomputeChallengeScoresCommandHandler(
        IChallengeRepository challenges,
        ChallengeScoreProjector projector,
        IDistributedCache cache,
        ILogger<RecomputeChallengeScoresCommandHandler> logger)
    {
        _challenges = challenges;
        _projector = projector;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<int>> HandleAsync(RecomputeChallengeScoresCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("RecomputeChallengeScoresCommand started");

        IReadOnlyList<Challenge> running = await _challenges.GetRunningTrackedAsync(ct);
        foreach (Challenge challenge in running)
        {
            await _projector.ProjectAsync(challenge, ct);
        }

        Result save = await _challenges.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("RecomputeChallengeScoresCommand failed to save: ErrorCode={ErrorCode}", save.Error.Code);
            return Result.Failure<int>(save.Error);
        }

        await ChallengeCache.InvalidateBoardsAsync(_cache, running.Select(c => c.Id), ct);
        _logger.LogInformation("RecomputeChallengeScoresCommand succeeded: Challenges={Challenges}", running.Count);
        return Result.Success(running.Count);
    }
}
