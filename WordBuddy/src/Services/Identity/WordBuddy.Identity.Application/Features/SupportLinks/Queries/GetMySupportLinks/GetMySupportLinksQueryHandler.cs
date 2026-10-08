using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetMySupportLinks;

/// <summary>Cache-aside read under <c>identity:supportlinks:{userId}</c> (absolute expiry). Link commands delete the key.</summary>
public sealed class GetMySupportLinksQueryHandler : IQueryHandler<GetMySupportLinksQuery, MySupportLinksDto>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly IDistributedCache _cache;
    private readonly SupportLinkOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<GetMySupportLinksQueryHandler> _logger;

    public GetMySupportLinksQueryHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        IDistributedCache cache,
        SupportLinkOptions options,
        TimeProvider time,
        ILogger<GetMySupportLinksQueryHandler> logger)
    {
        _users = users;
        _links = links;
        _cache = cache;
        _options = options;
        _time = time;
        _logger = logger;
    }

    public async Task<Result<MySupportLinksDto>> HandleAsync(GetMySupportLinksQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetMySupportLinksQuery started: UserId={UserId}", query.UserId);

        string key = SupportLinkCache.Key(query.UserId);
        string? cached = await _cache.GetStringAsync(key, ct);
        if (cached is not null)
        {
            MySupportLinksDto? hit = JsonSerializer.Deserialize<MySupportLinksDto>(cached, JsonOptions);
            if (hit is not null)
            {
                return Result.Success(hit);
            }
        }

        IReadOnlyList<SupportLink> mine = await _links.GetLinksForUserAsync(query.UserId, ct);

        // Children for whom the caller is the active Primary: their other links are managed here.
        List<Guid> childIds = mine
            .Where(l => l.SupporterId == query.UserId && l.IsPrimary && l.IsActive)
            .Select(l => l.LearnerId)
            .ToList();
        List<SupportLink> managed = [];
        foreach (Guid childId in childIds)
        {
            IReadOnlyList<SupportLink> childLinks = await _links.GetLearnerLinksAsync(childId, ct);
            managed.AddRange(childLinks.Where(l => l.SupporterId != query.UserId && l.Status != SupportLinkStatus.Revoked));
        }

        List<SupportLink> all = [.. mine, .. managed];
        IReadOnlyList<UnlinkRequest> openRequests = await _links.GetOpenUnlinkRequestsAsync(all.Select(l => l.Id).ToList(), ct);
        Dictionary<Guid, UnlinkRequest> requestByLink = openRequests.ToDictionary(r => r.LinkId);

        List<Guid> userIds = all.SelectMany(l => new[] { l.LearnerId, l.SupporterId }).Distinct().ToList();
        IReadOnlyList<User> users = await _users.GetByIdsAsync(userIds, ct);
        Dictionary<Guid, User> usersById = users.ToDictionary(u => u.Id);

        SupportLinkDto Map(SupportLink link) => SupportLinkDtoMapper.ToDto(
            link, usersById, requestByLink.GetValueOrDefault(link.Id), query.UserId, _options.UnlinkOverrideWaitDays);

        IReadOnlyList<SupportLinkInvitation> invitations =
            await _links.GetOpenInvitationsByCreatorAsync(query.UserId, _time.GetUtcNow().UtcDateTime, ct);

        MySupportLinksDto result = new(
            mine.Where(l => l.LearnerId == query.UserId).Select(Map).ToList(),
            mine.Where(l => l.SupporterId == query.UserId).Select(Map).ToList(),
            managed.Select(Map).ToList(),
            invitations.Select(i => new InvitationDto(i.Id, i.CreatorSide, i.Relationship, i.ExpiresAtUtc)).ToList(),
            mine.Any(l => l.LearnerId == query.UserId && l.IsActive));

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(result, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheMinutes) },
            ct);

        _logger.LogInformation(
            "GetMySupportLinksQuery succeeded: UserId={UserId}, LinkCount={LinkCount}", query.UserId, all.Count);
        return Result.Success(result);
    }
}
