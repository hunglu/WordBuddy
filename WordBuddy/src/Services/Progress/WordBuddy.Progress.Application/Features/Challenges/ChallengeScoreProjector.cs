using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;

namespace WordBuddy.Progress.Application.Features.Challenges;

/// <summary>
/// Rewrites <c>Score</c>, dense <c>Rank</c> and <c>TargetReached</c> of every participant of a challenge from the
/// review logs, using each learner time zone for the local day. Stages the changes (the caller saves).
/// The result depends only on the logs, so a rerun gives the same state.
/// </summary>
public sealed class ChallengeScoreProjector
{
    private readonly IChallengeRepository _challenges;
    private readonly IChallengeProfileRepository _profiles;
    private readonly IReviewLogRepository _reviewLogs;
    private readonly ChallengeOptions _options;
    private readonly ILogger<ChallengeScoreProjector> _logger;

    public ChallengeScoreProjector(
        IChallengeRepository challenges,
        IChallengeProfileRepository profiles,
        IReviewLogRepository reviewLogs,
        ChallengeOptions options,
        ILogger<ChallengeScoreProjector> logger)
    {
        _challenges = challenges;
        _profiles = profiles;
        _reviewLogs = reviewLogs;
        _options = options;
        _logger = logger;
    }

    /// <summary>Stages the projection of one challenge. Returns the number of participants projected.</summary>
    public async Task<int> ProjectAsync(Challenge challenge, CancellationToken ct = default)
    {
        IReadOnlyList<ChallengeParticipant> participants = await _challenges.GetParticipantsTrackedAsync(challenge.Id, ct);
        if (participants.Count == 0)
        {
            return 0;
        }

        List<Guid> userIds = [.. participants.Select(p => p.UserId)];
        Dictionary<Guid, ChallengeProfile> profiles = (await _profiles.GetByIdsAsync(userIds, ct)).ToDictionary(p => p.UserId);
        ILookup<Guid, ReviewLog> logs = (await _reviewLogs.GetForChallengeAsync(userIds, challenge.StartsAtUtc, challenge.EndsAtUtc, ct)).ToLookup(l => l.UserId);

        Dictionary<Guid, int> scores = [];
        foreach (ChallengeParticipant participant in participants)
        {
            string? timeZoneId = profiles.TryGetValue(participant.UserId, out ChallengeProfile? profile) ? profile.TimeZoneId : null;
            ChallengeScoringWindow window = new(challenge.StartsAtUtc, challenge.EndsAtUtc, participant.JoinedAtUtc);
            scores[participant.UserId] = ChallengeScorer.Score(logs[participant.UserId], challenge.Metric, window, timeZoneId, _options);
        }

        // Dense rank: equal scores share a rank and the next rank follows without a gap.
        List<int> distinctDescending = [.. scores.Values.Distinct().OrderByDescending(s => s)];
        foreach (ChallengeParticipant participant in participants)
        {
            int score = scores[participant.UserId];
            int rank = distinctDescending.IndexOf(score) + 1;
            bool reached = challenge.Kind == ChallengeKind.Target && challenge.Target is { } target && score >= target;
            participant.ApplyProjection(score, rank, reached);
        }

        _logger.LogInformation(
            "ChallengeScoreProjector projected: ChallengeId={ChallengeId}, Participants={Participants}", challenge.Id, participants.Count);
        return participants.Count;
    }
}
