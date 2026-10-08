using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetLearnerSupportLinksForAdmin;

public sealed class GetLearnerSupportLinksForAdminQueryHandler : IQueryHandler<GetLearnerSupportLinksForAdminQuery, IReadOnlyList<AdminSupportLinkDto>>
{
    private readonly ISupportLinkRepository _links;
    private readonly ILogger<GetLearnerSupportLinksForAdminQueryHandler> _logger;

    public GetLearnerSupportLinksForAdminQueryHandler(ISupportLinkRepository links, ILogger<GetLearnerSupportLinksForAdminQueryHandler> logger)
    {
        _links = links;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AdminSupportLinkDto>>> HandleAsync(GetLearnerSupportLinksForAdminQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetLearnerSupportLinksForAdminQuery started: LearnerId={LearnerId}", query.LearnerId);

        if (query.LearnerId == Guid.Empty)
        {
            return Result.Failure<IReadOnlyList<AdminSupportLinkDto>>(
                Error.Validation("GetLearnerSupportLinksForAdmin.Validation", "LearnerId is required."));
        }

        IReadOnlyList<SupportLink> links = await _links.GetLearnerLinksAsync(query.LearnerId, ct);
        IReadOnlyList<AdminSupportLinkDto> result = links
            .Select(l => new AdminSupportLinkDto(l.Id, l.LearnerId, l.SupporterId, l.IsPrimary, l.Relationship, l.Status, l.CreatedAtUtc))
            .ToList();

        return Result.Success(result);
    }
}
