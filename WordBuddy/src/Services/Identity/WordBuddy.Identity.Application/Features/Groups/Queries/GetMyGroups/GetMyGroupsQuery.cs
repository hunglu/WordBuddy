using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetMyGroups;

/// <summary>Groups the caller owns, and groups the caller is an active member of (name and owner only).</summary>
public sealed record GetMyGroupsQuery(Guid CallerId) : IQuery<MyGroupsDto>;
