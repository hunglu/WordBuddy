using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Users.Queries.GetUsersForAdmin;

/// <summary>Not cached: admin only, low traffic. Never logs the search text (it may be an email).</summary>
public sealed class GetUsersForAdminQueryHandler : IQueryHandler<GetUsersForAdminQuery, PagedResult<AdminUserDto>>
{
    private readonly IUserRepository _users;
    private readonly IValidator<GetUsersForAdminQuery> _validator;
    private readonly ILogger<GetUsersForAdminQueryHandler> _logger;

    public GetUsersForAdminQueryHandler(
        IUserRepository users,
        IValidator<GetUsersForAdminQuery> validator,
        ILogger<GetUsersForAdminQueryHandler> logger)
    {
        _users = users;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AdminUserDto>>> HandleAsync(GetUsersForAdminQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetUsersForAdminQuery started: SearchLength={SearchLength}, Page={Page}, PageSize={PageSize}",
            query.Search?.Length ?? 0, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetUsersForAdminQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<AdminUserDto>>(Error.Validation("GetUsersForAdmin.Validation", validation.ToString()));
        }

        string? search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        PagedResult<User> page = await _users.GetAdultsPagedAsync(search, query.Page, query.PageSize, ct);

        List<AdminUserDto> items = page.Items
            .Select(u => new AdminUserDto(u.Id, u.Email, u.DisplayName, u.IsAdmin, u.IsModerator, u.IsGuardian))
            .ToList();

        _logger.LogInformation("GetUsersForAdminQuery succeeded: Count={Count}, TotalCount={TotalCount}", items.Count, page.TotalCount);
        return Result.Success(new PagedResult<AdminUserDto>(items, page.Page, page.PageSize, page.TotalCount));
    }
}
