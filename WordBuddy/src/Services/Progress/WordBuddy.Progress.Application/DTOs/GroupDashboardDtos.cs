namespace WordBuddy.Progress.Application.DTOs;

// Dates only: no DTO here carries a time of day. Names are not in Progress: the UI joins LearnerId with Identity's group detail.

/// <summary>Dashboard of a learning group: one row per active member and group totals.</summary>
/// <param name="GroupId">The group.</param>
/// <param name="Days">Window length in local days (7, 30 or 90).</param>
/// <param name="From">First local day of the window.</param>
/// <param name="To">Last local day of the window.</param>
/// <param name="Totals">Group totals.</param>
/// <param name="Members">One row per active member whose support link to the owner is active.</param>
public sealed record GroupDashboardDto(
    Guid GroupId,
    int Days,
    DateOnly From,
    DateOnly To,
    GroupTotalsDto Totals,
    IReadOnlyList<GroupMemberDashboardDto> Members);

/// <summary>Group totals.</summary>
/// <param name="MemberCount">Members in the table.</param>
/// <param name="MedianRetention">Median of the members' true retention (percent), or <see langword="null"/> when no member has enough answers.</param>
/// <param name="MembersActiveThisWeek">Members with a review in the last 7 local days.</param>
public sealed record GroupTotalsDto(int MemberCount, double? MedianRetention, int MembersActiveThisWeek);

/// <summary>One member row of the group dashboard.</summary>
/// <param name="LearnerId">The learner.</param>
/// <param name="CurrentStreakDays">Consecutive local days with a review, ending today or yesterday.</param>
/// <param name="ActiveDays">Local days with a review inside the window.</param>
/// <param name="Retention">True retention (percent), or <see langword="null"/> below the minimum sample.</param>
/// <param name="RetentionSample">Answers behind the retention figure.</param>
/// <param name="PerStatus">Active words per status.</param>
/// <param name="LeechCount">Words with status Leech.</param>
/// <param name="LastActiveDate">Last local day with a review (looks back up to 90 days), or <see langword="null"/>.</param>
public sealed record GroupMemberDashboardDto(
    Guid LearnerId,
    int CurrentStreakDays,
    int ActiveDays,
    double? Retention,
    int RetentionSample,
    IReadOnlyList<WordStatusCountDto> PerStatus,
    int LeechCount,
    DateOnly? LastActiveDate);
