using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;

public sealed class GetUnlinkRequestsForAdminQueryHandler : IQueryHandler<GetUnlinkRequestsForAdminQuery, PagedResult<AdminUnlinkRequestDto>>
{
    private readonly ISupportLinkRepository _links;
    private readonly IValidator<GetUnlinkRequestsForAdminQuery> _validator;
    private readonly ILogger<GetUnlinkRequestsForAdminQueryHandler> _logger;

    public GetUnlinkRequestsForAdminQueryHandler(
        ISupportLinkRepository links,
        IValidator<GetUnlinkRequestsForAdminQuery> validator,
        ILogger<GetUnlinkRequestsForAdminQueryHandler> logger)
    {
        _links = links;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AdminUnlinkRequestDto>>> HandleAsync(GetUnlinkRequestsForAdminQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetUnlinkRequestsForAdminQuery started: Page={Page}, PageSize={PageSize}", query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetUnlinkRequestsForAdminQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<AdminUnlinkRequestDto>>(Error.Validation("GetUnlinkRequestsForAdmin.Validation", validation.ToString()));
        }

        PagedResult<UnlinkRequest> requests = await _links.GetEscalatedUnlinkRequestsAsync(query.Page, query.PageSize, ct);
        IReadOnlyList<SupportLink> links = await _links.GetLinksByIdsAsync(requests.Items.Select(r => r.LinkId).ToList(), ct);
        Dictionary<Guid, SupportLink> linkById = links.ToDictionary(l => l.Id);

        List<AdminUnlinkRequestDto> items = requests.Items
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

        _logger.LogInformation("GetUnlinkRequestsForAdminQuery succeeded: Count={Count}, TotalCount={TotalCount}", items.Count, requests.TotalCount);
        return Result.Success(new PagedResult<AdminUnlinkRequestDto>(items, requests.Page, requests.PageSize, requests.TotalCount));
    }
}
