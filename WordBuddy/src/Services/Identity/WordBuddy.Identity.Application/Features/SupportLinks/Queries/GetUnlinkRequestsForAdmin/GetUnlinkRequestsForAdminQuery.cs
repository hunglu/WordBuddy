using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;

/// <summary>
/// One page of unlink requests escalated to an admin (status OverrideRequested). Ids only.
/// <paramref name="IncludeChildLearners"/> is true for admins; a moderator sees adult learners only.
/// <paramref name="ExcludePartyUserId"/> hides requests on links where that user is the learner or supporter
/// (null for admins; the moderator's own id for a moderator).
/// </summary>
public sealed record GetUnlinkRequestsForAdminQuery(int Page, int PageSize, bool IncludeChildLearners, Guid? ExcludePartyUserId)
    : IQuery<PagedResult<AdminUnlinkRequestDto>>;
