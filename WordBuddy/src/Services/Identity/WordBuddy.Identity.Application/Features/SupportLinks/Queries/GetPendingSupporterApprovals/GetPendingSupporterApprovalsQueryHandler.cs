using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetPendingSupporterApprovals;

public sealed class GetPendingSupporterApprovalsQueryHandler : IQueryHandler<GetPendingSupporterApprovalsQuery, IReadOnlyList<SupportLinkDto>>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly SupportLinkOptions _options;
    private readonly ILogger<GetPendingSupporterApprovalsQueryHandler> _logger;

    public GetPendingSupporterApprovalsQueryHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        SupportLinkOptions options,
        ILogger<GetPendingSupporterApprovalsQueryHandler> logger)
    {
        _users = users;
        _links = links;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SupportLinkDto>>> HandleAsync(GetPendingSupporterApprovalsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetPendingSupporterApprovalsQuery started: UserId={UserId}", query.UserId);

        IReadOnlyList<SupportLink> pending = await _links.GetPendingForPrimaryAsync(query.UserId, ct);
        List<Guid> userIds = pending.SelectMany(l => new[] { l.LearnerId, l.SupporterId }).Distinct().ToList();
        IReadOnlyList<User> users = await _users.GetByIdsAsync(userIds, ct);
        Dictionary<Guid, User> usersById = users.ToDictionary(u => u.Id);

        IReadOnlyList<SupportLinkDto> result = pending
            .Select(l => SupportLinkDtoMapper.ToDto(l, usersById, null, query.UserId, _options.UnlinkOverrideWaitDays))
            .ToList();

        _logger.LogInformation("GetPendingSupporterApprovalsQuery succeeded: Count={Count}", result.Count);
        return Result.Success(result);
    }
}
