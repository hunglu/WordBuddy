using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.RemapVocabularyWordIds;

/// <summary>Rewrites recall stats from merged Content word ids to the surviving ids. Per user: with no
/// stat for the new id, the old stat is re-pointed in place; otherwise its counts are merged into the
/// new stat and the old stat is deleted. Idempotent — a second run finds no stats for the old ids.
/// Sessions hold only aggregate counts (no word ids), so they need no rewrite.
/// <para><b>Accepted limitation.</b> Progress does not keep the applied pairs. A recall result
/// submitted with a merged-away (pre-migration) word id <i>after</i> Content's remap was
/// acknowledged creates a stat under the old id that no later sync rewrites. The window is a UI
/// session left open across the deploy; closing it would need Progress to persist applied remaps
/// and map ids on submit — a design change, not made here.</para></summary>
public sealed class RemapVocabularyWordIdsCommandHandler : ICommandHandler<RemapVocabularyWordIdsCommand>
{
    private readonly IVocabularyRecallRepository _repository;
    private readonly IValidator<RemapVocabularyWordIdsCommand> _validator;
    private readonly ILogger<RemapVocabularyWordIdsCommandHandler> _logger;

    public RemapVocabularyWordIdsCommandHandler(
        IVocabularyRecallRepository repository,
        IValidator<RemapVocabularyWordIdsCommand> validator,
        ILogger<RemapVocabularyWordIdsCommandHandler> logger)
    {
        _repository = repository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RemapVocabularyWordIdsCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("RemapVocabularyWordIdsCommand started: Count={Count}", command.Remaps?.Count ?? 0);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RemapVocabularyWordIdsCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RemapVocabularyWordIds.Validation", validation.ToString()));
        }

        if (command.Remaps.Count == 0)
        {
            return Result.Success();
        }

        Dictionary<Guid, Guid> newIdByOldId = command.Remaps.ToDictionary(r => r.OldId, r => r.NewId);
        HashSet<Guid> involvedIds = [.. newIdByOldId.Keys, .. newIdByOldId.Values];

        Result<IReadOnlyList<VocabularyRecallStat>> statsResult = await _repository.GetTrackedStatsByWordIdsAsync(involvedIds, ct);
        if (statsResult.IsFailure)
        {
            _logger.LogWarning(
                "RemapVocabularyWordIdsCommand repository failure: {ErrorCode} — {ErrorDescription}",
                statsResult.Error.Code, statsResult.Error.Description);
            return statsResult;
        }

        // Current holder of each (user, surviving word id) — starts with stats already on a new id,
        // and picks up re-pointed stats so two old ids merging into one new id also merge.
        Dictionary<(Guid UserId, Guid WordId), VocabularyRecallStat> targets = statsResult.Value
            .Where(s => !newIdByOldId.ContainsKey(s.VocabularyWordId))
            .ToDictionary(s => (s.UserId, s.VocabularyWordId));

        List<VocabularyRecallStat> removed = [];
        int rewritten = 0;

        foreach (VocabularyRecallStat stat in statsResult.Value.Where(s => newIdByOldId.ContainsKey(s.VocabularyWordId)))
        {
            Guid newId = newIdByOldId[stat.VocabularyWordId];

            if (targets.TryGetValue((stat.UserId, newId), out VocabularyRecallStat? target))
            {
                target.MergeFrom(stat);
                removed.Add(stat);
            }
            else
            {
                stat.RemapWordId(newId);
                targets[(stat.UserId, newId)] = stat;
                rewritten++;
            }
        }

        Result saveResult = await _repository.SaveRemappedStatsAsync(removed, ct);
        if (saveResult.IsFailure)
        {
            _logger.LogWarning(
                "RemapVocabularyWordIdsCommand failed to persist: {ErrorCode} — {ErrorDescription}",
                saveResult.Error.Code, saveResult.Error.Description);
            return saveResult;
        }

        _logger.LogInformation(
            "RemapVocabularyWordIdsCommand succeeded: Rewritten={Rewritten}, Merged={Merged}",
            rewritten, removed.Count);
        return Result.Success();
    }
}
