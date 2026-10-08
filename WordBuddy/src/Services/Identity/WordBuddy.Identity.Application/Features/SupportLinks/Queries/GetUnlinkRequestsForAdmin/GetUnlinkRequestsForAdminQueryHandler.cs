using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;

public sealed class GetUnlinkRequestsForAdminQueryHandler : IQueryHandler<GetUnlinkRequestsForAdminQuery, IReadOnlyList<AdminUnlinkRequestDto>>
{
    private readonly ISupportLinkRepository _links;
    private readonly ILogger<GetUnlinkRequestsForAdminQueryHandler> _logger;

    public GetUnlinkRequestsForAdminQueryHandler(ISupportLinkRepository links, ILogger<GetUnlinkRequestsForAdminQueryHandler> logger)
    {
        _links = links;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AdminUnlinkRequestDto>>> HandleAsync(GetUnlinkRequestsForAdminQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetUnlinkRequestsForAdminQuery started");

        IReadOnlyList<UnlinkRequest> requests = await _links.GetEscalatedUnlinkRequestsAsync(ct);
        IReadOnlyList<SupportLink> links = await _links.GetLinksByIdsAsync(requests.Select(r => r.LinkId).ToList(), ct);
        Dictionary<Guid, SupportLink> linkById = links.ToDictionary(l => l.Id);

        IReadOnlyList<AdminUnlinkRequestDto> result = requests
            .Where(r => linkById.ContainsKey(r.LinkId))
            .Select(r => new AdminUnlinkRequestDto(
                r.Id,
                r.LinkId,
                linkById[r.LinkId].LearnerId,
                linkById[r.LinkId].SupporterId,
                r.RequestedById,
                r.RequestedBySide,
                r.Status,
                r.RequestedAtUtc,
                r.EscalatedAtUtc))
            .ToList();

        _logger.LogInformation("GetUnlinkRequestsForAdminQuery succeeded: Count={Count}", result.Count);
        return Result.Success(result);
    }
}
