using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetGroup;

/// <summary>
/// Cache-aside read under <c>identity:group:{groupId}</c> (absolute expiry). The owner check runs on
/// every call, including cache hits. Members show the public name and avatar only; a child without
/// alias appears as "Child learner".
/// </summary>
public sealed class GetGroupQueryHandler : IQueryHandler<GetGroupQuery, LearnerGroupDetailDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IUserRepository _users;
    private readonly ILearnerGroupRepository _groups;
    private readonly IDistributedCache _cache;
    private readonly LearnerGroupOptions _options;
    private readonly ILogger<GetGroupQueryHandler> _logger;

    public GetGroupQueryHandler(
        IUserRepository users,
        ILearnerGroupRepository groups,
        IDistributedCache cache,
        LearnerGroupOptions options,
        ILogger<GetGroupQueryHandler> logger)
    {
        _users = users;
        _groups = groups;
        _cache = cache;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<LearnerGroupDetailDto>> HandleAsync(GetGroupQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetGroupQuery started: GroupId={GroupId}, CallerId={CallerId}", query.GroupId, query.CallerId);

        string key = LearnerGroupCache.Key(query.GroupId);
        string? cached = await _cache.GetStringAsync(key, ct);
        if (cached is not null)
        {
            LearnerGroupDetailDto? hit = JsonSerializer.Deserialize<LearnerGroupDetailDto>(cached, JsonOptions);
            if (hit is not null)
            {
                return CheckOwner(hit, query);
            }
        }

        Result<LearnerGroup> found = await _groups.GetGroupAsync(query.GroupId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<LearnerGroupDetailDto>(found.Error);
        }

        LearnerGroup group = found.Value;
        IReadOnlyList<LearnerGroupMember> members = (await _groups.GetOpenMembersAsync([group.Id], ct))
            .OrderBy(m => m.AddedAtUtc)
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

        LearnerGroupDetailDto detail = new(group.Id, group.OwnerId, group.Name, group.CreatedAtUtc, memberDtos);
        Result<LearnerGroupDetailDto> checkedResult = CheckOwner(detail, query);
        if (checkedResult.IsFailure)
        {
            return checkedResult;
        }

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(detail, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheMinutes) },
            ct);

        _logger.LogInformation("GetGroupQuery succeeded: GroupId={GroupId}, MemberCount={MemberCount}", group.Id, memberDtos.Count);
        return checkedResult;
    }

    private Result<LearnerGroupDetailDto> CheckOwner(LearnerGroupDetailDto detail, GetGroupQuery query)
    {
        if (detail.OwnerId == query.CallerId)
        {
            return Result.Success(detail);
        }

        _logger.LogWarning("GetGroupQuery rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", query.GroupId, LearnerGroupErrors.NotOwner.Code);
        return Result.Failure<LearnerGroupDetailDto>(LearnerGroupErrors.NotOwner);
    }
}
