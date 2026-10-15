using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Application.Features.Challenges.Queries.GetChallengeBoard;

public sealed class GetChallengeBoardQueryValidator : AbstractValidator<GetChallengeBoardQuery>
{
    public GetChallengeBoardQueryValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.ChallengeId).NotEmpty();
        RuleFor(q => q.AgeGroup).IsInEnum();
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
