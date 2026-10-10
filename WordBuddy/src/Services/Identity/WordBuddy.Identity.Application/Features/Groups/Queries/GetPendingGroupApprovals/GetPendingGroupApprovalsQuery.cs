using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetPendingGroupApprovals;

/// <summary>Pending child memberships waiting for the caller (as Primary supporter) to approve.</summary>
public sealed record GetPendingGroupApprovalsQuery(Guid CallerId) : IQuery<IReadOnlyList<PendingGroupApprovalDto>>;
