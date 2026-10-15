using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetGroup;

/// <summary>
/// Reads the group snapshot (cached under <c>identity:group-snapshot:{groupId}</c>). The owner check
/// runs on every call, including cache hits. The detail has member counts and ids only; the members
/// themselves come from <c>GetGroupMembersQuery</c>.
/// </summary>
public sealed class GetGroupQueryHandler : IQueryHandler<GetGroupQuery, LearnerGroupDetailDto>
{
    private readonly LearnerGroupSnapshotLoader _snapshots;
    private readonly ILogger<GetGroupQueryHandler> _logger;

    public GetGroupQueryHandler(LearnerGroupSnapshotLoader snapshots, ILogger<GetGroupQueryHandler> logger)
    {
        _snapshots = snapshots;
        _logger = logger;
    }

    public async Task<Result<LearnerGroupDetailDto>> HandleAsync(GetGroupQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetGroupQuery started: GroupId={GroupId}, CallerId={CallerId}", query.GroupId, query.CallerId);

        Result<LearnerGroupSnapshot> loaded = await _snapshots.LoadAsync(query.GroupId, ct);
        if (loaded.IsFailure)
        {
            return Result.Failure<LearnerGroupDetailDto>(loaded.Error);
        }

        LearnerGroupSnapshot snapshot = loaded.Value;
        if (snapshot.OwnerId != query.CallerId)
        {
            _logger.LogWarning("GetGroupQuery rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", query.GroupId, LearnerGroupErrors.NotOwner.Code);
            return Result.Failure<LearnerGroupDetailDto>(LearnerGroupErrors.NotOwner);
        }

        LearnerGroupDetailDto detail = new(
            snapshot.Id,
            snapshot.OwnerId,
            snapshot.Name,
            snapshot.CreatedAtUtc,
            snapshot.Members.Count(m => m.Status == GroupMemberStatus.Active),
            snapshot.Members.Count(m => m.Status == GroupMemberStatus.PendingPrimaryApproval),
            snapshot.Members.Select(m => m.LearnerId).ToList());

        _logger.LogInformation("GetGroupQuery succeeded: GroupId={GroupId}, MemberCount={MemberCount}", snapshot.Id, snapshot.Members.Count);
        return Result.Success(detail);
    }
}
