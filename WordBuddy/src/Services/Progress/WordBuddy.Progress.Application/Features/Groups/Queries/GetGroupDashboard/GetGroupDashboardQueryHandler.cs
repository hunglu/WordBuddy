using System.Globalization;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.Dashboard;
using WordBuddy.Progress.Application.Features.VocabularySrs;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Groups.Queries.GetGroupDashboard;

/// <summary>
/// Builds the group dashboard with one batched read per metric, cached 5 minutes (absolute) under
/// <c>progress:dashboard:group:{groupId}:{days}:{offsetMinutes}</c>. No explicit delete. Only active members
/// whose support link to the owner is still active appear, so a stale group event never leaks data.
/// Same result for child and adult members. Logs ids and counts only.
/// </summary>
public sealed class GetGroupDashboardQueryHandler : IQueryHandler<GetGroupDashboardQuery, GroupDashboardDto>
{
    private readonly ILearnerGroupProjectionRepository _groups;
    private readonly ISupportLinkProjectionRepository _supportLinks;
    private readonly IDashboardReadRepository _repository;
    private readonly DashboardCalculator _calculator;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<GetGroupDashboardQuery> _validator;
    private readonly ILogger<GetGroupDashboardQueryHandler> _logger;

    public GetGroupDashboardQueryHandler(
        ILearnerGroupProjectionRepository groups,
        ISupportLinkProjectionRepository supportLinks,
        IDashboardReadRepository repository,
        DashboardCalculator calculator,
        IDistributedCache cache,
        TimeProvider timeProvider,
        IValidator<GetGroupDashboardQuery> validator,
        ILogger<GetGroupDashboardQueryHandler> logger)
    {
        _groups = groups;
        _supportLinks = supportLinks;
        _repository = repository;
        _calculator = calculator;
        _cache = cache;
        _timeProvider = timeProvider;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Cache key for one group, window and client offset.</summary>
    public static string CacheKey(Guid groupId, int days, TimeSpan offset) =>
        $"progress:dashboard:group:{groupId}:{days}:{((int)offset.TotalMinutes).ToString(CultureInfo.InvariantCulture)}";

    public async Task<Result<GroupDashboardDto>> HandleAsync(GetGroupDashboardQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetGroupDashboardQuery started: GroupId={GroupId}, CallerId={CallerId}, Days={Days}", query.GroupId, query.CallerId, query.Days);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetGroupDashboardQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<GroupDashboardDto>(Error.Validation("GetGroupDashboard.Validation", validation.ToString()));
        }

        if (!await _groups.IsOwnerAsync(query.GroupId, query.CallerId, ct))
        {
            _logger.LogWarning("GetGroupDashboardQuery rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", query.GroupId, GroupDashboardErrors.Forbidden.Code);
            return Result.Failure<GroupDashboardDto>(GroupDashboardErrors.Forbidden);
        }

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        Result<TimeSpan> offsetResult = ClientDateTime.ParseOffset(query.ClientCurrentDateTime, nowUtc);
        if (offsetResult.IsFailure)
        {
            _logger.LogWarning("GetGroupDashboardQuery invalid client date-time header: GroupId={GroupId}", query.GroupId);
            return Result.Failure<GroupDashboardDto>(offsetResult.Error);
        }

        TimeSpan offset = offsetResult.Value;
        string cacheKey = CacheKey(query.GroupId, query.Days, offset);

        string? cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            GroupDashboardDto? hit = JsonSerializer.Deserialize<GroupDashboardDto>(cached);
            if (hit is not null)
            {
                _logger.LogInformation("GetGroupDashboardQuery cache hit: GroupId={GroupId}, Days={Days}", query.GroupId, query.Days);
                return Result.Success(hit);
            }
        }

        IReadOnlyList<LearnerGroupMemberProjection> rows = await _groups.GetGroupAsync(query.GroupId, ct);
        List<Guid> activeIds = rows.Where(r => r.IsActive).Select(r => r.LearnerId).ToList();
        IReadOnlySet<Guid> supported = await _supportLinks.GetLearnersWithActiveLinkAsync(query.CallerId, activeIds, ct);
        List<Guid> memberIds = activeIds.Where(supported.Contains).ToList();

        (DateTime fromUtc, DateTime toUtc) = DashboardCalculator.SourceRange(nowUtc, offset);

        Result<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardReviewRow>>> reviews =
            await _repository.GetReviewLogsForUsersAsync(memberIds, fromUtc, toUtc, ct);
        if (reviews.IsFailure)
        {
            return Failed(query, reviews.Error);
        }

        Result<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardWordRow>>> words =
            await _repository.GetWordStatesForUsersAsync(memberIds, ct);
        if (words.IsFailure)
        {
            return Failed(query, words.Error);
        }

        Result<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardMembershipRow>>> memberships =
            await _repository.GetMembershipsForUsersAsync(memberIds, fromUtc, toUtc, ct);
        if (memberships.IsFailure)
        {
            return Failed(query, memberships.Error);
        }

        Result<IReadOnlyDictionary<Guid, IReadOnlyList<DashboardSessionRow>>> sessions =
            await _repository.GetSessionIssuesForUsersAsync(memberIds, fromUtc, toUtc, ct);
        if (sessions.IsFailure)
        {
            return Failed(query, sessions.Error);
        }

        GroupDashboardDto dashboard = _calculator.CalculateGroup(
            query.GroupId, query.Days, nowUtc, offset, memberIds, reviews.Value, words.Value, memberships.Value, sessions.Value);

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(dashboard),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
            ct);

        _logger.LogInformation(
            "GetGroupDashboardQuery succeeded: GroupId={GroupId}, Days={Days}, MemberCount={MemberCount}",
            query.GroupId, query.Days, dashboard.Members.Count);
        return Result.Success(dashboard);
    }

    private Result<GroupDashboardDto> Failed(GetGroupDashboardQuery query, Error error)
    {
        _logger.LogWarning(
            "GetGroupDashboardQuery failed to read data: GroupId={GroupId}, ErrorCode={ErrorCode}", query.GroupId, error.Code);
        return Result.Failure<GroupDashboardDto>(error);
    }
}
