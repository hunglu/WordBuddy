using FluentValidation;
using WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;

namespace WordBuddy.Progress.Application.Features.Groups.Queries.GetGroupDashboard;

/// <summary>Validates <see cref="GetGroupDashboardQuery"/>.</summary>
public sealed class GetGroupDashboardQueryValidator : AbstractValidator<GetGroupDashboardQuery>
{
    public GetGroupDashboardQueryValidator()
    {
        RuleFor(q => q.GroupId).NotEmpty();
        RuleFor(q => q.CallerId).NotEmpty();
        RuleFor(q => q.Days)
            .Must(days => GetLearnerDashboardQueryValidator.AllowedDays.Contains(days))
            .WithMessage("'Days' must be 7, 30 or 90.");
    }
}
