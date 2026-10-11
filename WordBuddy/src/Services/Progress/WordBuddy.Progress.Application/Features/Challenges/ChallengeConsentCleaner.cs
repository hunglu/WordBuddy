using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges;

/// <inheritdoc />
public sealed class ChallengeConsentCleaner : IChallengeConsentCleaner
{
    private readonly IChallengeRepository _challenges;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ChallengeConsentCleaner> _logger;

    public ChallengeConsentCleaner(IChallengeRepository challenges, IDistributedCache cache, ILogger<ChallengeConsentCleaner> logger)
    {
        _challenges = challenges;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> RemoveForLinkAsync(Guid learnerId, Guid supporterId, CancellationToken ct = default) =>
        await RemoveAsync(learnerId, supporterId, ct);

    /// <inheritdoc />
    public async Task<Result> RemoveByGuardianAsync(Guid guardianId, CancellationToken ct = default) =>
        await RemoveAsync(null, guardianId, ct);

    /// <inheritdoc />
    public async Task<Result> RemoveAllForChildAsync(Guid childId, CancellationToken ct = default) =>
        await RemoveAsync(childId, null, ct);

    private async Task<Result> RemoveAsync(Guid? childId, Guid? enabledBy, CancellationToken ct)
    {
        IReadOnlyList<ChallengeConsent> consents = await _challenges.GetConsentsTrackedAsync(childId, enabledBy, ct);
        if (consents.Count == 0)
        {
            return Result.Success();
        }

        _challenges.RemoveConsents(consents);

        // A child keeps playing while another guardian consent is left.
        List<Guid> affectedChildren = [.. consents.Select(c => c.ChildId).Distinct()];
        List<Guid> changedChallenges = [];
        foreach (Guid child in affectedChildren)
        {
            IReadOnlyList<ChallengeConsent> all = await _challenges.GetConsentsTrackedAsync(child, null, ct);
            bool hasOther = all.Any(c => !consents.Any(r => r.ChildId == c.ChildId && r.EnabledBy == c.EnabledBy));
            if (hasOther)
            {
                continue;
            }

            IReadOnlyList<ChallengeParticipant> participations = await _challenges.GetUnfinishedParticipationsTrackedAsync(child, ct);
            foreach (ChallengeParticipant participation in participations)
            {
                _challenges.RemoveParticipant(participation);
                changedChallenges.Add(participation.ChallengeId);
            }
        }

        Result save = await _challenges.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("ChallengeConsentCleaner failed to save: ErrorCode={ErrorCode}", save.Error.Code);
            return save;
        }

        await ChallengeCache.InvalidateBoardsAsync(_cache, changedChallenges, ct);
        _logger.LogInformation(
            "ChallengeConsentCleaner removed consents: Consents={Consents}, ChildrenAffected={Children}, ChallengesChanged={Challenges}",
            consents.Count, affectedChildren.Count, changedChallenges.Distinct().Count());
        return Result.Success();
    }
}
