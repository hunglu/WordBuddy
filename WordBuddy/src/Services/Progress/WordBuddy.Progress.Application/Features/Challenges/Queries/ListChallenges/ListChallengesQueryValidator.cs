using FluentValidation;

namespace WordBuddy.Progress.Application.Features.Challenges.Queries.ListChallenges;

public sealed class ListChallengesQueryValidator : AbstractValidator<ListChallengesQuery>
{
    public ListChallengesQueryValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.AgeGroup).IsInEnum();
    }
}
