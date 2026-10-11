using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.AdvanceChallengeLifecycle;

/// <summary>
/// 1. For each template and audience, creates the next window(s) (never more than one future instance, so no
/// overflow instance). 2. Moves each challenge forward by time: Scheduled to Open to Running to Finished.
/// The scores are recomputed once more before a challenge is finished. Safe to rerun.
/// </summary>
public sealed class AdvanceChallengeLifecycleCommandHandler : ICommandHandler<AdvanceChallengeLifecycleCommand, int>
{
    private readonly IChallengeRepository _challenges;
    private readonly ChallengeScoreProjector _projector;
    private readonly IDistributedCache _cache;
    private readonly ChallengeOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<AdvanceChallengeLifecycleCommandHandler> _logger;

    public AdvanceChallengeLifecycleCommandHandler(
        IChallengeRepository challenges,
        ChallengeScoreProjector projector,
        IDistributedCache cache,
        ChallengeOptions options,
        TimeProvider time,
        ILogger<AdvanceChallengeLifecycleCommandHandler> logger)
    {
        _challenges = challenges;
        _projector = projector;
        _cache = cache;
        _options = options;
        _time = time;
        _logger = logger;
    }

    public async Task<Result<int>> HandleAsync(AdvanceChallengeLifecycleCommand command, CancellationToken ct = default)
    {
        DateTime now = _time.GetUtcNow().UtcDateTime;
        _logger.LogInformation("AdvanceChallengeLifecycleCommand started");

        int created = await CreateDueInstancesAsync(now, ct);
        if (created > 0)
        {
            Result savedNew = await _challenges.SaveChangesAsync(ct);
            if (savedNew.IsFailure)
            {
                _logger.LogWarning("AdvanceChallengeLifecycleCommand failed to save new instances: ErrorCode={ErrorCode}", savedNew.Error.Code);
                return Result.Failure<int>(savedNew.Error);
            }
        }

        int moved = 0;
        List<Guid> finished = [];
        foreach (Challenge challenge in await _challenges.GetNotFinishedTrackedAsync(ct))
        {
            moved += await MoveForwardAsync(challenge, now, finished, ct);
        }

        if (moved > 0)
        {
            Result save = await _challenges.SaveChangesAsync(ct);
            if (save.IsFailure)
            {
                _logger.LogWarning("AdvanceChallengeLifecycleCommand failed to save: ErrorCode={ErrorCode}", save.Error.Code);
                return Result.Failure<int>(save.Error);
            }

            await ChallengeCache.InvalidateBoardsAsync(_cache, finished, ct);
        }

        _logger.LogInformation("AdvanceChallengeLifecycleCommand succeeded: Created={Created}, StatusChanges={Moved}", created, moved);
        return Result.Success(created + moved);
    }

    private async Task<int> CreateDueInstancesAsync(DateTime now, CancellationToken ct)
    {
        int created = 0;
        TimeSpan openLead = TimeSpan.FromHours(_options.OpenLeadHours);

        foreach (ChallengeTemplate template in ChallengeTemplates.All)
        {
            foreach (ChallengeAudience audience in template.Audiences)
            {
                Result<Challenge> latest = await _challenges.GetLatestAsync(template.Key, audience, ct);
                IReadOnlyList<ChallengeWindow> windows = template.NextWindows(
                    latest.IsSuccess ? latest.Value.StartsAtUtc : null,
                    latest.IsSuccess ? latest.Value.EndsAtUtc : null,
                    now,
                    openLead);

                foreach (ChallengeWindow window in windows)
                {
                    Result<Challenge> instance = Challenge.Create(
                        Guid.NewGuid(), template.Key, audience, template.Kind, template.Metric, template.Target,
                        window.OpensAtUtc, window.StartsAtUtc, window.EndsAtUtc, _options.Capacity);
                    if (instance.IsFailure)
                    {
                        _logger.LogWarning("AdvanceChallengeLifecycleCommand could not create an instance: TemplateKey={TemplateKey}, ErrorCode={ErrorCode}", template.Key, instance.Error.Code);
                        continue;
                    }

                    await _challenges.AddAsync(instance.Value, ct);
                    created++;
                    _logger.LogInformation(
                        "Challenge created: TemplateKey={TemplateKey}, Audience={Audience}, ChallengeId={ChallengeId}", template.Key, audience, instance.Value.Id);
                }
            }
        }

        return created;
    }

    private async Task<int> MoveForwardAsync(Challenge challenge, DateTime now, List<Guid> finished, CancellationToken ct)
    {
        int moves = 0;
        while (true)
        {
            Result step;
            if (challenge.Status == ChallengeStatus.Scheduled && now >= challenge.OpensAtUtc)
            {
                step = challenge.Open();
            }
            else if (challenge.Status == ChallengeStatus.Open && now >= challenge.StartsAtUtc)
            {
                step = challenge.Start();
            }
            else if (challenge.Status == ChallengeStatus.Running && now >= challenge.EndsAtUtc)
            {
                await _projector.ProjectAsync(challenge, ct);
                step = challenge.Finish();
                if (step.IsSuccess)
                {
                    finished.Add(challenge.Id);
                }
            }
            else
            {
                return moves;
            }

            if (step.IsFailure)
            {
                _logger.LogWarning("Challenge transition failed: ChallengeId={ChallengeId}, ErrorCode={ErrorCode}", challenge.Id, step.Error.Code);
                return moves;
            }

            moves++;
        }
    }
}
