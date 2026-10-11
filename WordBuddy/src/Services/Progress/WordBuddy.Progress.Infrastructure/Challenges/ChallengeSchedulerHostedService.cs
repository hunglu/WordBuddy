using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Features.Challenges.Commands.AdvanceChallengeLifecycle;
using WordBuddy.Progress.Application.Features.Challenges.Commands.RecomputeChallengeScores;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Challenges;

/// <summary>
/// Background clock of the challenges. Every tick (default 60 s) it runs the life cycle (create next instances, move
/// statuses); every <c>RecomputeIntervalMinutes</c> (default 5) it also rewrites the scores. A failed handler is logged
/// and retried on the next tick; the loop never stops because of one failure. Disabled with <c>Challenges:SchedulerEnabled=false</c>.
/// </summary>
public sealed class ChallengeSchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ChallengeOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<ChallengeSchedulerHostedService> _logger;
    private DateTime _lastRecomputeUtc = DateTime.MinValue;

    public ChallengeSchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        ChallengeOptions options,
        TimeProvider time,
        ILogger<ChallengeSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _time = time;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.SchedulerEnabled)
        {
            _logger.LogInformation("ChallengeScheduler disabled by configuration");
            return;
        }

        TimeSpan tick = TimeSpan.FromSeconds(Math.Max(1, _options.LifecycleTickSeconds));
        _logger.LogInformation(
            "ChallengeScheduler started: TickSeconds={TickSeconds}, RecomputeMinutes={RecomputeMinutes}",
            tick.TotalSeconds, _options.RecomputeIntervalMinutes);

        using PeriodicTimer timer = new(tick);
        try
        {
            do
            {
                await RunTickAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    /// <summary>Runs one tick. Public so a test can drive it without waiting for the timer.</summary>
    public async Task RunTickAsync(CancellationToken ct)
    {
        await RunAsync<AdvanceChallengeLifecycleCommand>(new AdvanceChallengeLifecycleCommand(), "lifecycle", ct);

        DateTime now = _time.GetUtcNow().UtcDateTime;
        if (now - _lastRecomputeUtc >= TimeSpan.FromMinutes(Math.Max(1, _options.RecomputeIntervalMinutes)))
        {
            if (await RunAsync<RecomputeChallengeScoresCommand>(new RecomputeChallengeScoresCommand(), "recompute", ct))
            {
                _lastRecomputeUtc = now;
            }
        }
    }

    private async Task<bool> RunAsync<TCommand>(TCommand command, string step, CancellationToken ct)
        where TCommand : ICommand<int>
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ICommandHandler<TCommand, int> handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, int>>();
            Result<int> result = await handler.HandleAsync(command, ct);
            if (result.IsFailure)
            {
                _logger.LogWarning("ChallengeScheduler {Step} failed: ErrorCode={ErrorCode}", step, result.Error.Code);
                return false;
            }

            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Survive a failing tick (database not ready, transient error): the next tick tries again.
            _logger.LogError(ex, "ChallengeScheduler {Step} threw", step);
            return false;
        }
    }
}
