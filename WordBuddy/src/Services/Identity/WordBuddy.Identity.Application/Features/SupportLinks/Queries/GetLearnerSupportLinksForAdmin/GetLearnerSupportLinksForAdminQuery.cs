using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetLearnerSupportLinksForAdmin;

/// <summary>All links of one learner, for the admin Primary handover. Ids only.</summary>
public sealed record GetLearnerSupportLinksForAdminQuery(Guid LearnerId) : IQuery<IReadOnlyList<AdminSupportLinkDto>>;
