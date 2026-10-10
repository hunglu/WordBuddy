using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.Groups.Queries.GetGroupDashboard;

/// <summary>Dashboard of a group for its owner. The <c>CanManageGroup</c> policy checks the owner before this runs;
/// the handler checks again. <paramref name="ClientCurrentDateTime"/> is the raw <c>X-Client-CurrentDateTime</c> header.
/// <paramref name="Days"/> is 7, 30 or 90.</summary>
public sealed record GetGroupDashboardQuery(Guid GroupId, Guid CallerId, string? ClientCurrentDateTime, int Days) : IQuery<GroupDashboardDto>;
