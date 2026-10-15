using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetLearnerSupportLinksForAdmin;

public sealed class GetLearnerSupportLinksForAdminQueryHandler : IQueryHandler<GetLearnerSupportLinksForAdminQuery, PagedResult<AdminSupportLinkDto>>
{
    private readonly ISupportLinkRepository _links;
    private readonly IValidator<GetLearnerSupportLinksForAdminQuery> _validator;
    private readonly ILogger<GetLearnerSupportLinksForAdminQueryHandler> _logger;

    public GetLearnerSupportLinksForAdminQueryHandler(
        ISupportLinkRepository links,
        IValidator<GetLearnerSupportLinksForAdminQuery> validator,
        ILogger<GetLearnerSupportLinksForAdminQueryHandler> logger)
    {
        _links = links;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AdminSupportLinkDto>>> HandleAsync(GetLearnerSupportLinksForAdminQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetLearnerSupportLinksForAdminQuery started: LearnerId={LearnerId}, Page={Page}, PageSize={PageSize}",
            query.LearnerId, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetLearnerSupportLinksForAdminQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<AdminSupportLinkDto>>(
                Error.Validation("GetLearnerSupportLinksForAdmin.Validation", validation.ToString()));
        }

        PagedResult<SupportLink> links = await _links.GetLearnerLinksPagedAsync(query.LearnerId, query.Page, query.PageSize, ct);
        List<AdminSupportLinkDto> items = links.Items
            .Select(l => new AdminSupportLinkDto(l.Id, l.LearnerId, l.SupporterId, l.IsPrimary, l.Relationship, l.Status, l.CreatedAtUtc))
            .ToList();

        _logger.LogInformation("GetLearnerSupportLinksForAdminQuery succeeded: Count={Count}, TotalCount={TotalCount}", items.Count, links.TotalCount);
        return Result.Success(new PagedResult<AdminSupportLinkDto>(items, links.Page, links.PageSize, links.TotalCount));
    }
}
