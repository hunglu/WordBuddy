using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;

/// <summary>One page of unlink requests escalated to an admin (status OverrideRequested). Ids only.</summary>
public sealed record GetUnlinkRequestsForAdminQuery(int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<AdminUnlinkRequestDto>>;
