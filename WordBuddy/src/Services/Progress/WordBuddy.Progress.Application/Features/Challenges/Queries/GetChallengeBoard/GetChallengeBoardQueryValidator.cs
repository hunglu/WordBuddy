using FluentValidation;

namespace WordBuddy.Progress.Application.Features.Challenges.Queries.GetChallengeBoard;

public sealed class GetChallengeBoardQueryValidator : AbstractValidator<GetChallengeBoardQuery>
{
    public GetChallengeBoardQueryValidator()
    {
        RuleFor(q => q.UserId).NotEmpty();
        RuleFor(q => q.ChallengeId).NotEmpty();
        RuleFor(q => q.AgeGroup).IsInEnum();
    }
}
