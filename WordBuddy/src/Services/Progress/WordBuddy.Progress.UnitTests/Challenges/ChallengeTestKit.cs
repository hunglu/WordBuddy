using Microsoft.Extensions.Caching.Distributed;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Challenges;

/// <summary>Builders and in-memory fakes for the WB-29 tests.</summary>
internal static class ChallengeTestKit
{
    /// <summary>A Monday 00:00 UTC.</summary>
    public static readonly DateTime Monday = new(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);

    public static Challenge NewChallenge(
        ChallengeAudience audience = ChallengeAudience.Adult,
        ChallengeKind kind = ChallengeKind.Ranked,
        ChallengeMetric metric = ChallengeMetric.MemoryPoints,
        int? target = null,
        ChallengeStatus status = ChallengeStatus.Open,
        int capacity = 30,
        string templateKey = ChallengeTemplates.WeeklyLeagueKey,
        DateTime? startsAtUtc = null)
    {
        DateTime starts = startsAtUtc ?? Monday;
        Challenge challenge = Challenge.Create(
            Guid.NewGuid(), templateKey, audience, kind, metric, kind == ChallengeKind.Target ? target ?? 7 : null,
            starts.AddDays(-1), starts, starts.AddDays(7), capacity).Value;

        if (status >= ChallengeStatus.Open)
        {
            challenge.Open();
        }

        if (status >= ChallengeStatus.Running)
        {
            challenge.Start();
        }

        if (status >= ChallengeStatus.Finished)
        {
            challenge.Finish();
        }

        return challenge;
    }

    public static ReviewLog Log(
        Guid userId,
        Guid senseId,
        DateTime at,
        ExerciseType type = ExerciseType.Typing,
        bool correct = true,
        bool due = true,
        int attempt = 1,
        bool hint = false,
        int ms = 3000,
        bool adjusted = false) =>
        ReviewLog.Create(
            Guid.NewGuid(), userId, senseId, Guid.NewGuid(), at, type,
            type == ExerciseType.ListeningChoice ? VocabularySkill.Listening : VocabularySkill.Spelling,
            correct, ms, hint, due, attempt, FsrsRating.Good, null, null, null, adjusted);

    public static ChallengeProfile Profile(Guid userId, AgeGroup ageGroup, string? alias = null, string? timeZoneId = null, bool guardian = false, string? displayName = null) =>
        ChallengeProfile.Create(userId, ageGroup, alias, "owl", displayName, timeZoneId, guardian, Monday);
}

/// <summary>A <see cref="IDistributedCache"/> in a dictionary. Expiry is not simulated.</summary>
internal sealed class FakeDistributedCache : IDistributedCache
{
    public Dictionary<string, byte[]> Items { get; } = [];

    public byte[]? Get(string key) => Items.GetValueOrDefault(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => Items[key] = value;

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        Set(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
    }

    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

    public void Remove(string key) => Items.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        Remove(key);
        return Task.CompletedTask;
    }
}

/// <summary>An in-memory <see cref="IChallengeRepository"/>. Tracked and read-only calls return the same instances.</summary>
internal sealed class FakeChallengeRepository : IChallengeRepository
{
    public List<Challenge> Challenges { get; } = [];

    public List<ChallengeParticipant> Participants { get; } = [];

    public List<ChallengeConsent> Consents { get; } = [];

    public int SaveCount { get; private set; }

    public int ReadBoardCount { get; private set; }

    public Task<Result<Challenge>> GetByIdAsync(Guid challengeId, CancellationToken ct = default)
    {
        Challenge? challenge = Challenges.FirstOrDefault(c => c.Id == challengeId);
        return Task.FromResult(challenge is null ? Result.Failure<Challenge>(ChallengeErrors.NotFound) : Result.Success(challenge));
    }

    public Task<Result<Challenge>> GetTrackedByIdAsync(Guid challengeId, CancellationToken ct = default) => GetByIdAsync(challengeId, ct);

    public Task<PagedResult<Challenge>> GetActiveAsync(ChallengeAudience audience, int page, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(Paging.Slice<Challenge>(
            [.. Challenges.Where(c => c.Audience == audience && c.Status is ChallengeStatus.Open or ChallengeStatus.Running).OrderBy(c => c.StartsAtUtc)],
            page,
            pageSize));

    public Task<IReadOnlyList<Challenge>> GetNotFinishedTrackedAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Challenge>>([.. Challenges.Where(c => c.Status != ChallengeStatus.Finished)]);

    public Task<IReadOnlyList<Challenge>> GetRunningTrackedAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Challenge>>([.. Challenges.Where(c => c.Status == ChallengeStatus.Running)]);

    public Task<Result<Challenge>> GetLatestAsync(string templateKey, ChallengeAudience audience, CancellationToken ct = default)
    {
        Challenge? latest = Challenges.Where(c => c.TemplateKey == templateKey && c.Audience == audience).OrderByDescending(c => c.StartsAtUtc).FirstOrDefault();
        return Task.FromResult(latest is null ? Result.Failure<Challenge>(ChallengeErrors.NotFound) : Result.Success(latest));
    }

    public Task AddAsync(Challenge challenge, CancellationToken ct = default)
    {
        Challenges.Add(challenge);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChallengeParticipant>> GetParticipantsAsync(Guid challengeId, CancellationToken ct = default)
    {
        ReadBoardCount++;
        return Task.FromResult<IReadOnlyList<ChallengeParticipant>>([.. Participants.Where(p => p.ChallengeId == challengeId)]);
    }

    public Task<IReadOnlyList<ChallengeParticipant>> GetParticipantsTrackedAsync(Guid challengeId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ChallengeParticipant>>([.. Participants.Where(p => p.ChallengeId == challengeId)]);

    public Task<Result<ChallengeParticipant>> GetParticipantTrackedAsync(Guid challengeId, Guid userId, CancellationToken ct = default)
    {
        ChallengeParticipant? participant = Participants.FirstOrDefault(p => p.ChallengeId == challengeId && p.UserId == userId);
        return Task.FromResult(participant is null
            ? Result.Failure<ChallengeParticipant>(Error.NotFound("Challenge.NotJoined", "not joined"))
            : Result.Success(participant));
    }

    public Task<IReadOnlyDictionary<Guid, int>> GetParticipantCountsAsync(IReadOnlyCollection<Guid> challengeIds, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(
            Participants.Where(p => challengeIds.Contains(p.ChallengeId)).GroupBy(p => p.ChallengeId).ToDictionary(g => g.Key, g => g.Count()));

    public Task<IReadOnlyList<ChallengeParticipant>> GetParticipationsAsync(Guid userId, IReadOnlyCollection<Guid> challengeIds, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ChallengeParticipant>>([.. Participants.Where(p => p.UserId == userId && challengeIds.Contains(p.ChallengeId))]);

    public Task<IReadOnlyList<ChallengeParticipant>> GetUnfinishedParticipationsTrackedAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ChallengeParticipant>>(
            [.. Participants.Where(p => p.UserId == userId && Challenges.Any(c => c.Id == p.ChallengeId && c.Status != ChallengeStatus.Finished))]);

    public Task<Result> AddParticipantIfRoomAsync(ChallengeParticipant participant, int capacity, CancellationToken ct = default)
    {
        if (Participants.Count(p => p.ChallengeId == participant.ChallengeId) >= capacity)
        {
            return Task.FromResult(Result.Failure(ChallengeErrors.Full));
        }

        Participants.Add(participant);
        return Task.FromResult(Result.Success());
    }

    public void RemoveParticipant(ChallengeParticipant participant) => Participants.Remove(participant);

    public Task<bool> HasConsentAsync(Guid childId, CancellationToken ct = default) => Task.FromResult(Consents.Any(c => c.ChildId == childId));

    public Task<IReadOnlyList<ChallengeConsent>> GetConsentsTrackedAsync(Guid? childId, Guid? enabledBy, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ChallengeConsent>>(
            [.. Consents.Where(c => (childId is null || c.ChildId == childId) && (enabledBy is null || c.EnabledBy == enabledBy))]);

    public Task AddConsentAsync(ChallengeConsent consent, CancellationToken ct = default)
    {
        Consents.Add(consent);
        return Task.CompletedTask;
    }

    public void RemoveConsents(IEnumerable<ChallengeConsent> consents)
    {
        foreach (ChallengeConsent consent in consents.ToList())
        {
            Consents.Remove(consent);
        }
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.FromResult(Result.Success());
    }
}

/// <summary>An in-memory <see cref="IChallengeProfileRepository"/>.</summary>
internal sealed class FakeChallengeProfileRepository : IChallengeProfileRepository
{
    public Dictionary<Guid, ChallengeProfile> Profiles { get; } = [];

    public Task<Result<ChallengeProfile>> GetAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(Profiles.TryGetValue(userId, out ChallengeProfile? profile)
            ? Result.Success(profile)
            : Result.Failure<ChallengeProfile>(Error.NotFound("ChallengeProfile.NotFound", "none")));

    public Task<Result<ChallengeProfile>> GetTrackedAsync(Guid userId, CancellationToken ct = default) => GetAsync(userId, ct);

    public Task<IReadOnlyList<ChallengeProfile>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ChallengeProfile>>([.. Profiles.Values.Where(p => userIds.Contains(p.UserId))]);

    public Task AddAsync(ChallengeProfile profile, CancellationToken ct = default)
    {
        Profiles[profile.UserId] = profile;
        return Task.CompletedTask;
    }

    public Task<Result> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(Result.Success());
}
