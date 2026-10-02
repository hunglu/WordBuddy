using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.RemapVocabularyWordIds;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SyncVocabularyWordIdRemaps;

/// <summary>Fetch a batch from Content → apply it with <see cref="RemapVocabularyWordIdsCommand"/> →
/// acknowledge it, repeating while the batch was full (at most <see cref="MaxBatchesPerRun"/> batches
/// per run). A batch whose remap fails is never acknowledged, so Content serves it again next run;
/// re-applying an already-applied batch is a no-op, so a crash between remap and ack is safe.</summary>
public sealed class SyncVocabularyWordIdRemapsCommandHandler : ICommandHandler<SyncVocabularyWordIdRemapsCommand, int>
{
    /// <summary>Upper bound on batches per run so one run can never spin forever.</summary>
    public const int MaxBatchesPerRun = 50;

    private readonly IContentVocabularyRemapClient _contentClient;
    private readonly ICommandHandler<RemapVocabularyWordIdsCommand> _remapHandler;
    private readonly IValidator<SyncVocabularyWordIdRemapsCommand> _validator;
    private readonly ILogger<SyncVocabularyWordIdRemapsCommandHandler> _logger;

    public SyncVocabularyWordIdRemapsCommandHandler(
        IContentVocabularyRemapClient contentClient,
        ICommandHandler<RemapVocabularyWordIdsCommand> remapHandler,
        IValidator<SyncVocabularyWordIdRemapsCommand> validator,
        ILogger<SyncVocabularyWordIdRemapsCommandHandler> logger)
    {
        _contentClient = contentClient;
        _remapHandler = remapHandler;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<int>> HandleAsync(SyncVocabularyWordIdRemapsCommand command, CancellationToken ct = default)
    {
        _logger.LogDebug("SyncVocabularyWordIdRemapsCommand started: BatchSize={BatchSize}", command.BatchSize);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("SyncVocabularyWordIdRemapsCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<int>(Error.Validation("SyncVocabularyWordIdRemaps.Validation", validation.ToString()));
        }

        int totalApplied = 0;
        int batches = 0;

        while (batches < MaxBatchesPerRun)
        {
            Result<IReadOnlyList<VocabularyWordIdRemapPair>> pendingResult = await _contentClient.GetPendingAsync(command.BatchSize, ct);
            if (pendingResult.IsFailure)
            {
                _logger.LogWarning(
                    "SyncVocabularyWordIdRemapsCommand failed to fetch remaps: {ErrorCode} — {ErrorDescription}",
                    pendingResult.Error.Code, pendingResult.Error.Description);
                return Result.Failure<int>(pendingResult.Error);
            }

            IReadOnlyList<VocabularyWordIdRemapPair> batch = pendingResult.Value;
            if (batch.Count == 0)
            {
                break;
            }

            RemapVocabularyWordIdsCommand remapCommand = new(batch.Select(p => new VocabularyWordIdRemap(p.OldId, p.NewId)).ToList());
            Result remapResult = await _remapHandler.HandleAsync(remapCommand, ct);
            if (remapResult.IsFailure)
            {
                _logger.LogWarning(
                    "SyncVocabularyWordIdRemapsCommand remap failed, batch not acknowledged: {ErrorCode} — {ErrorDescription}",
                    remapResult.Error.Code, remapResult.Error.Description);
                return Result.Failure<int>(remapResult.Error);
            }

            List<Guid> oldIds = batch.Select(p => p.OldId).ToList();
            Result ackResult = await _contentClient.AcknowledgeAsync(oldIds, ct);
            if (ackResult.IsFailure)
            {
                _logger.LogWarning(
                    "SyncVocabularyWordIdRemapsCommand failed to acknowledge: {ErrorCode} — {ErrorDescription}",
                    ackResult.Error.Code, ackResult.Error.Description);
                return Result.Failure<int>(ackResult.Error);
            }

            totalApplied += batch.Count;
            batches++;

            if (batch.Count < command.BatchSize)
            {
                break;
            }
        }

        if (totalApplied > 0)
        {
            _logger.LogInformation(
                "SyncVocabularyWordIdRemapsCommand applied {Count} remaps in {Batches} batches",
                totalApplied, batches);
        }
        else
        {
            _logger.LogDebug("SyncVocabularyWordIdRemapsCommand: no pending remaps");
        }

        return Result.Success(totalApplied);
    }
}
