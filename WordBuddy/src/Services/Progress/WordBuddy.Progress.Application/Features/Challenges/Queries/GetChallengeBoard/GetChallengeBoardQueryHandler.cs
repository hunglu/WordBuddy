using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Progress.Domain.Challenges;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Queries.GetChallengeBoard;

/// <summary>
/// Builds the board from a cached ranking (<c>progress:challenge-board:{id}</c>, absolute 5 minutes). Filtering for
/// the caller happens after the cache read. Adult: one page of the full ranking. Child: the caller row and the top 5
/// (paging ignored, one page), alias and avatar only, no user ids, scores only on the caller row. Logs ids and counts only.
/// </summary>
public sealed class GetChallengeBoardQueryHandler : IQueryHandler<GetChallengeBoardQuery, ChallengeBoardDto>
{
    /// <summary>Rows a child sees besides their own.</summary>
    public const int ChildTopCount = 5;

    private readonly IChallengeRepository _challenges;
    private readonly IChallengeProfileRepository _profiles;
    private readonly IDistributedCache _cache;
    private readonly IValidator<GetChallengeBoardQuery> _validator;
    private readonly ILogger<GetChallengeBoardQueryHandler> _logger;

    public GetChallengeBoardQueryHandler(
        IChallengeRepository challenges,
        IChallengeProfileRepository profiles,
        IDistributedCache cache,
        IValidator<GetChallengeBoardQuery> validator,
        ILogger<GetChallengeBoardQueryHandler> logger)
    {
        _challenges = challenges;
        _profiles = profiles;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<ChallengeBoardDto>> HandleAsync(GetChallengeBoardQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetChallengeBoardQuery started: ChallengeId={ChallengeId}, UserId={UserId}, Page={Page}, PageSize={PageSize}",
            query.ChallengeId, query.UserId, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetChallengeBoardQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<ChallengeBoardDto>(Error.Validation("GetChallengeBoard.Validation", validation.ToString()));
        }

        Result<Challenge> found = await _challenges.GetByIdAsync(query.ChallengeId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<ChallengeBoardDto>(found.Error);
        }

        Challenge challenge = found.Value;
        ChallengeAudience audience = ChallengeAudiences.From(query.AgeGroup);
        if (audience != challenge.Audience)
        {
            _logger.LogWarning("GetChallengeBoardQuery audience mismatch: ChallengeId={ChallengeId}", query.ChallengeId);
            return Result.Failure<ChallengeBoardDto>(ChallengeErrors.AudienceMismatch);
        }

        if (audience == ChallengeAudience.Child && !await _challenges.HasConsentAsync(query.UserId, ct))
        {
            _logger.LogWarning("GetChallengeBoardQuery: child without consent, UserId={UserId}", query.UserId);
            return Result.Failure<ChallengeBoardDto>(ChallengeErrors.NoConsent);
        }

        BoardSnapshot snapshot = await GetSnapshotAsync(challenge, ct);
        ChallengeBoardDto board = Filter(challenge, snapshot, query, audience);

        _logger.LogInformation(
            "GetChallengeBoardQuery succeeded: ChallengeId={ChallengeId}, Entries={Entries}, TotalCount={TotalCount}",
            query.ChallengeId, board.Entries.Items.Count, board.Entries.TotalCount);
        return Result.Success(board);
    }

    private async Task<BoardSnapshot> GetSnapshotAsync(Challenge challenge, CancellationToken ct)
    {
        string key = ChallengeCache.BoardKey(challenge.Id);
        string? cached = await _cache.GetStringAsync(key, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            BoardSnapshot? hit = JsonSerializer.Deserialize<BoardSnapshot>(cached);
            if (hit is not null)
            {
                return hit;
            }
        }

        IReadOnlyList<ChallengeParticipant> participants = await _challenges.GetParticipantsAsync(challenge.Id, ct);
        Dictionary<Guid, ChallengeProfile> profiles =
            (await _profiles.GetByIdsAsync([.. participants.Select(p => p.UserId)], ct)).ToDictionary(p => p.UserId);

        List<BoardSnapshotRow> rows = [.. participants
            .OrderBy(p => p.Rank ?? int.MaxValue).ThenByDescending(p => p.Score).ThenBy(p => p.JoinedAtUtc).ThenBy(p => p.Id)
            .Select(p =>
            {
                bool known = profiles.TryGetValue(p.UserId, out ChallengeProfile? profile);
                string name = known
                    ? profile!.PublicName
                    : challenge.Audience == ChallengeAudience.Child ? ChallengeProfile.ChildFallbackName : ChallengeProfile.AdultFallbackName;
                return new BoardSnapshotRow(p.UserId, p.Rank, p.Score, p.TargetReached, name, known ? profile!.AvatarId : null);
            })];

        BoardSnapshot snapshot = new(participants.Count, rows);
        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(snapshot),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ChallengeCache.BoardLifetime },
            ct);
        return snapshot;
    }

    private static ChallengeBoardDto Filter(Challenge challenge, BoardSnapshot snapshot, GetChallengeBoardQuery query, ChallengeAudience audience)
    {
        Guid userId = query.UserId;
        BoardSnapshotRow? mine = snapshot.Rows.FirstOrDefault(r => r.UserId == userId);
        PagedResult<ChallengeBoardEntryDto> entries;

        if (audience == ChallengeAudience.Adult)
        {
            List<ChallengeBoardEntryDto> all = [.. snapshot.Rows.Select(r => new ChallengeBoardEntryDto(r.Rank, r.Name, r.AvatarId, r.UserId == userId, r.Score, r.TargetReached))];
            entries = Paging.Slice(all, query.Page, query.PageSize);
        }
        else
        {
            // Child viewer: top 5 plus the caller row; no user ids and no scores of other children.
            IEnumerable<BoardSnapshotRow> visible = snapshot.Rows.Take(ChildTopCount);
            if (mine is not null && !snapshot.Rows.Take(ChildTopCount).Contains(mine))
            {
                visible = visible.Append(mine);
            }

            List<ChallengeBoardEntryDto> childRows = [.. visible.Select(r =>
            {
                bool isMe = r.UserId == userId;
                return new ChallengeBoardEntryDto(r.Rank, r.Name, r.AvatarId, isMe, isMe ? r.Score : null, isMe ? r.TargetReached : null);
            })];

            // Paging is ignored: one page, never more than the top 5 and the own row.
            entries = new PagedResult<ChallengeBoardEntryDto>(childRows, 1, ChildTopCount + 1, childRows.Count);
        }

        return new ChallengeBoardDto(
            challenge.Id,
            ChallengeTemplates.NameOf(challenge.TemplateKey),
            challenge.Status,
            challenge.Kind,
            challenge.Metric,
            challenge.Target,
            snapshot.ParticipantCount,
            mine?.Rank,
            mine?.Score,
            entries);
    }
}
