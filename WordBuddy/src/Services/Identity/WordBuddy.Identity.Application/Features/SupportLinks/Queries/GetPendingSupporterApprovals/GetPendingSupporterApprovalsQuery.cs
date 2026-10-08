using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetPendingSupporterApprovals;

/// <summary>Extra supporters waiting for the caller (as Primary) to approve.</summary>
public sealed record GetPendingSupporterApprovalsQuery(Guid UserId) : IQuery<IReadOnlyList<SupportLinkDto>>;
