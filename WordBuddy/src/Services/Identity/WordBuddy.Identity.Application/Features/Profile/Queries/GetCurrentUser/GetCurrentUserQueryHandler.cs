using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly ILogger<GetCurrentUserQueryHandler> _logger;

    public GetCurrentUserQueryHandler(IUserRepository users, ISupportLinkRepository links, ILogger<GetCurrentUserQueryHandler> logger)
    {
        _users = users;
        _links = links;
        _logger = logger;
    }

    public async Task<Result<UserDto>> HandleAsync(GetCurrentUserQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetCurrentUserQuery started: UserId={UserId}", query.UserId);

        Result<User> user = await _users.GetByIdAsync(query.UserId, ct);
        if (user.IsFailure)
        {
            return Result.Failure<UserDto>(user.Error);
        }

        bool hasActiveSupporter = await _links.HasActiveSupporterAsync(query.UserId, ct);
        return Result.Success(UserDto.From(user.Value, hasActiveSupporter));
    }
}
