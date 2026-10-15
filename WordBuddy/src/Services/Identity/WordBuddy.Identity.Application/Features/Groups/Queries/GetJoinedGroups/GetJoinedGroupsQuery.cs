using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetJoinedGroups;

/// <summary>One page of the groups the caller is an active member of (name and owner only).</summary>
public sealed record GetJoinedGroupsQuery(Guid CallerId, int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<JoinedGroupDto>>;
