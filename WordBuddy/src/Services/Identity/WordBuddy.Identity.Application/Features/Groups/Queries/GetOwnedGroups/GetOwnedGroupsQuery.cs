using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetOwnedGroups;

/// <summary>One page of the groups the caller owns, oldest first, with member counts. A child owns none.</summary>
public sealed record GetOwnedGroupsQuery(Guid CallerId, int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<OwnedGroupDto>>;
