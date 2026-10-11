using FluentValidation;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.LeaveChallenge;

public sealed class LeaveChallengeCommandValidator : AbstractValidator<LeaveChallengeCommand>
{
    public LeaveChallengeCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.ChallengeId).NotEmpty();
    }
}
