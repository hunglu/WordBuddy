using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetGroupMembers;

/// <summary>One page of the pending and active members of a group, oldest first. Owner only.</summary>
public sealed record GetGroupMembersQuery(Guid GroupId, Guid CallerId, int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<GroupMemberDto>>;
