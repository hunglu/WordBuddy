using FluentValidation;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.JoinChallenge;

public sealed class JoinChallengeCommandValidator : AbstractValidator<JoinChallengeCommand>
{
    public JoinChallengeCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ChallengeId).NotEmpty();
        RuleFor(c => c.AgeGroup).IsInEnum();
    }
}
