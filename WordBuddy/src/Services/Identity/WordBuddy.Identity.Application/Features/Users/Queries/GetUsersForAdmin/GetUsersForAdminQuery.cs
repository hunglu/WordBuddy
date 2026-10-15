using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Users.Queries.GetUsersForAdmin;

/// <summary>One page of adult accounts for role assignment, optionally filtered by email or display name.</summary>
public sealed record GetUsersForAdminQuery(string? Search, int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<AdminUserDto>>;
