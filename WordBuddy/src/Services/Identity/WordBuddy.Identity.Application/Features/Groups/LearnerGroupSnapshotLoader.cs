using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups;

/// <summary>
/// Cache-aside read of a <see cref="LearnerGroupSnapshot"/> (absolute expiry). It does no owner
/// check: the calling handler must run it on every call, including cache hits. Members show the
/// public name and avatar only; a child without alias appears as "Child learner".
/// </summary>
public sealed class LearnerGroupSnapshotLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IUserRepository _users;
    private readonly ILearnerGroupRepository _groups;
    private readonly IDistributedCache _cache;
    private readonly LearnerGroupOptions _options;
    private readonly ILogger<LearnerGroupSnapshotLoader> _logger;

    public LearnerGroupSnapshotLoader(
        IUserRepository users,
        ILearnerGroupRepository groups,
        IDistributedCache cache,
        LearnerGroupOptions options,
        ILogger<LearnerGroupSnapshotLoader> logger)
    {
        _users = users;
        _groups = groups;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    /// <summary>Returns the snapshot of a live group, or <see cref="LearnerGroupErrors.NotFound"/>.</summary>
    /// <param name="groupId">Group id.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<Result<LearnerGroupSnapshot>> LoadAsync(Guid groupId, CancellationToken ct = default)
    {
        string key = LearnerGroupCache.Key(groupId);
        string? cached = await _cache.GetStringAsync(key, ct);
        if (cached is not null)
        {
            LearnerGroupSnapshot? hit = JsonSerializer.Deserialize<LearnerGroupSnapshot>(cached, JsonOptions);
            if (hit is not null)
            {
                _logger.LogDebug("Group snapshot cache hit: GroupId={GroupId}", groupId);
                return Result.Success(hit);
            }
        }

        Result<LearnerGroup> found = await _groups.GetGroupAsync(groupId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<LearnerGroupSnapshot>(found.Error);
        }

        LearnerGroup group = found.Value;
        IReadOnlyList<LearnerGroupMember> members = (await _groups.GetOpenMembersAsync([group.Id], ct))
            .OrderBy(m => m.AddedAtUtc)
            .ThenBy(m => m.Id)
            .ToList();
        IReadOnlyList<User> users = await _users.GetByIdsAsync(members.Select(m => m.LearnerId).ToList(), ct);
        Dictionary<Guid, User> usersById = users.ToDictionary(u => u.Id);

        List<GroupMemberDto> memberDtos = [];
        foreach (LearnerGroupMember member in members)
        {
            if (usersById.TryGetValue(member.LearnerId, out User? user))
            {
                memberDtos.Add(new GroupMemberDto(
                    member.Id, member.LearnerId, user.PublicName, user.AvatarId, user.AgeGroup, member.Status, member.AddedAtUtc));
            }
        }

        LearnerGroupSnapshot snapshot = new(group.Id, group.OwnerId, group.Name, group.CreatedAtUtc, memberDtos);
        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(snapshot, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheMinutes) },
            ct);

        _logger.LogDebug("Group snapshot loaded: GroupId={GroupId}, MemberCount={MemberCount}", group.Id, memberDtos.Count);
        return Result.Success(snapshot);
    }
}
