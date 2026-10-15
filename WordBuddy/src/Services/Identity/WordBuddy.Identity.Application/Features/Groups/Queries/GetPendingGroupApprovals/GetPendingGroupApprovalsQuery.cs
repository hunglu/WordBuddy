using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetPendingGroupApprovals;

/// <summary>One page of the pending child memberships waiting for the caller (as Primary supporter) to approve.</summary>
public sealed record GetPendingGroupApprovalsQuery(Guid CallerId, int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<PendingGroupApprovalDto>>;
