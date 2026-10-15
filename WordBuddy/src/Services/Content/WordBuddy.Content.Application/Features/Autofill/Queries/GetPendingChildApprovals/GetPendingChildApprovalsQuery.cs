using WordBuddy.Content.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Autofill.Queries.GetPendingChildApprovals;

/// <summary>One page of auto-filled words waiting for child approval. <paramref name="LearnerId"/> set =
/// supporter queue of that learner (endpoint policy <c>CanSupportLearner</c>); <see langword="null"/> =
/// admin queue (policy <c>AdminOnly</c>).</summary>
public sealed record GetPendingChildApprovalsQuery(Guid? LearnerId, int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<ChildApprovalDto>>;
