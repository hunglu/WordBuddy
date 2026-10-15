using WordBuddy.Content.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Groups.Queries.GetGroupWordAssignments;

/// <summary>One page of the assignment history of a group, newest first. Owner only.</summary>
public sealed record GetGroupWordAssignmentsQuery(
    Guid GroupId,
    Guid CallerId,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IQuery<PagedResult<GroupWordAssignmentDto>>;
