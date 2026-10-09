using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.Autofill.Queries.GetPendingChildApprovals;

/// <summary>Auto-filled words waiting for child approval. <paramref name="LearnerId"/> set = supporter
/// queue of that learner (endpoint policy <c>CanSupportLearner</c>); <see langword="null"/> = admin queue
/// (policy <c>AdminOnly</c>).</summary>
public sealed record GetPendingChildApprovalsQuery(Guid? LearnerId) : IQuery<IReadOnlyList<ChildApprovalDto>>;
