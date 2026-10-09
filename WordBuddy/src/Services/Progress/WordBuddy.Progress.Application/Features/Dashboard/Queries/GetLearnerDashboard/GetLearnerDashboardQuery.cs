using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;

/// <summary>Dashboard of <paramref name="LearnerId"/>. The caller is the learner (<c>me</c>) or an active
/// supporter; the policy checks that before this runs. <paramref name="ClientCurrentDateTime"/> is the raw
/// <c>X-Client-CurrentDateTime</c> header (→ UTC day when missing). <paramref name="Days"/> is 7, 30 or 90.</summary>
public sealed record GetLearnerDashboardQuery(Guid LearnerId, string? ClientCurrentDateTime, int Days) : IQuery<LearnerDashboardDto>;
