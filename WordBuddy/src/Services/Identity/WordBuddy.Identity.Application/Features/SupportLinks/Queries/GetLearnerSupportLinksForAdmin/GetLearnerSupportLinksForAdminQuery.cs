using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetLearnerSupportLinksForAdmin;

/// <summary>One page of the links of one learner, for the admin Primary handover. Ids only.</summary>
public sealed record GetLearnerSupportLinksForAdminQuery(Guid LearnerId, int Page = 1, int PageSize = Paging.DefaultPageSize)
    : IQuery<PagedResult<AdminSupportLinkDto>>;
