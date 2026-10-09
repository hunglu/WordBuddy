using FluentValidation;

namespace WordBuddy.Progress.Application.Features.Dashboard.Queries.GetLearnerDashboard;

/// <summary>Validates <see cref="GetLearnerDashboardQuery"/>.</summary>
public sealed class GetLearnerDashboardQueryValidator : AbstractValidator<GetLearnerDashboardQuery>
{
    /// <summary>Window lengths a caller may ask for.</summary>
    public static readonly IReadOnlyList<int> AllowedDays = [7, 30, 90];

    public GetLearnerDashboardQueryValidator()
    {
        RuleFor(q => q.LearnerId).NotEmpty();
        RuleFor(q => q.Days)
            .Must(days => AllowedDays.Contains(days))
            .WithMessage("'Days' must be 7, 30 or 90.");
    }
}
