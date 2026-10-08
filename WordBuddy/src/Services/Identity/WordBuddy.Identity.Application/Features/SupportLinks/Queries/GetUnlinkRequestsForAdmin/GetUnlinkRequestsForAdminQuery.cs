using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;

/// <summary>Unlink requests escalated to an admin (status OverrideRequested). Ids only.</summary>
public sealed record GetUnlinkRequestsForAdminQuery : IQuery<IReadOnlyList<AdminUnlinkRequestDto>>;
