using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SyncVocabularyWordIdRemaps;
using WordBuddy.Progress.Infrastructure.Settings;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Services;

/// <summary>Pulls Content's vocabulary word-id remaps into Progress: waits
/// <see cref="ContentApiSettings.RemapInitialDelay"/>, then runs
/// <see cref="SyncVocabularyWordIdRemapsCommand"/> in a fresh DI scope every
/// <see cref="ContentApiSettings.RemapPollInterval"/>. Failures are logged and retried on the next
/// tick — the host never stops. Does nothing when <see cref="ContentApiSettings.RemapSyncEnabled"/>
/// is <see langword="false"/>. Not part of <c>/health/ready</c>: the sync is eventually consistent.</summary>
public sealed class VocabularyIdRemapSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ContentApiSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VocabularyIdRemapSyncService> _logger;

    public VocabularyIdRemapSyncService(
        IServiceScopeFactory scopeFactory,
        IOptions<ContentApiSettings> settings,
        TimeProvider timeProvider,
        ILogger<VocabularyIdRemapSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.RemapSyncEnabled)
        {
            _logger.LogInformation("Vocabulary remap sync is disabled (ContentApi:RemapSyncEnabled = false)");
            return;
        }

        try
        {
            await Task.Delay(_settings.RemapInitialDelay, _timeProvider, stoppingToken);

            using PeriodicTimer timer = new(_settings.RemapPollInterval, _timeProvider);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutting down.
        }
    }

    /// <summary>One sync run; never throws except for host shutdown.</summary>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            ICommandHandler<SyncVocabularyWordIdRemapsCommand, int> handler =
                scope.ServiceProvider.GetRequiredService<ICommandHandler<SyncVocabularyWordIdRemapsCommand, int>>();

            Result<int> result = await handler.HandleAsync(new SyncVocabularyWordIdRemapsCommand(), ct);
            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Vocabulary remap sync failed: {ErrorCode} — {ErrorDescription}; retrying in {PollInterval}",
                    result.Error.Code, result.Error.Description, _settings.RemapPollInterval);
            }
            else if (result.Value > 0)
            {
                _logger.LogInformation("Vocabulary remap sync applied {Count} remaps", result.Value);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vocabulary remap sync threw an unexpected exception; retrying in {PollInterval}", _settings.RemapPollInterval);
        }
    }
}
