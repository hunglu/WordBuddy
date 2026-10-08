using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetMySupportLinks;

/// <summary>All links of the caller (as learner, as supporter, managed for children as Primary) plus open invitations.</summary>
public sealed record GetMySupportLinksQuery(Guid UserId) : IQuery<MySupportLinksDto>;
