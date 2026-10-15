using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetPendingGroupApprovals;

public sealed class GetPendingGroupApprovalsQueryHandler : IQueryHandler<GetPendingGroupApprovalsQuery, PagedResult<PendingGroupApprovalDto>>
{
    private readonly IUserRepository _users;
    private readonly ILearnerGroupRepository _groups;
    private readonly IValidator<GetPendingGroupApprovalsQuery> _validator;
    private readonly ILogger<GetPendingGroupApprovalsQueryHandler> _logger;

    public GetPendingGroupApprovalsQueryHandler(
        IUserRepository users,
        ILearnerGroupRepository groups,
        IValidator<GetPendingGroupApprovalsQuery> validator,
        ILogger<GetPendingGroupApprovalsQueryHandler> logger)
    {
        _users = users;
        _groups = groups;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<PendingGroupApprovalDto>>> HandleAsync(GetPendingGroupApprovalsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetPendingGroupApprovalsQuery started: CallerId={CallerId}, Page={Page}, PageSize={PageSize}", query.CallerId, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetPendingGroupApprovalsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<PendingGroupApprovalDto>>(Error.Validation("GetPendingGroupApprovals.Validation", validation.ToString()));
        }

        PagedResult<LearnerGroupMember> pending = await _groups.GetPendingForPrimaryAsync(query.CallerId, query.Page, query.PageSize, ct);
        IReadOnlyList<LearnerGroup> groups = await _groups.GetGroupsByIdsAsync(pending.Items.Select(m => m.GroupId).Distinct().ToList(), ct);
        Dictionary<Guid, LearnerGroup> groupsById = groups.ToDictionary(g => g.Id);

        List<Guid> userIds = pending.Items.Select(m => m.LearnerId).Concat(groups.Select(g => g.OwnerId)).Distinct().ToList();
        IReadOnlyList<User> users = await _users.GetByIdsAsync(userIds, ct);
        Dictionary<Guid, User> usersById = users.ToDictionary(u => u.Id);

        List<PendingGroupApprovalDto> items = [];
        foreach (LearnerGroupMember member in pending.Items)
        {
            if (!groupsById.TryGetValue(member.GroupId, out LearnerGroup? group) || !usersById.TryGetValue(member.LearnerId, out User? learner))
            {
                continue;
            }

            usersById.TryGetValue(group.OwnerId, out User? owner);
            items.Add(new PendingGroupApprovalDto(
                member.Id, group.Id, group.Name, owner?.PublicName ?? "Supporter", learner.Id, learner.PublicName, learner.AvatarId, member.AddedAtUtc));
        }

        _logger.LogInformation("GetPendingGroupApprovalsQuery succeeded: Count={Count}, TotalCount={TotalCount}", items.Count, pending.TotalCount);
        return Result.Success(new PagedResult<PendingGroupApprovalDto>(items, pending.Page, pending.PageSize, pending.TotalCount));
    }
}
