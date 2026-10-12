using FluentValidation;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.SetChildChallengeConsent;

public sealed class SetChildChallengeConsentCommandValidator : AbstractValidator<SetChildChallengeConsentCommand>
{
    public SetChildChallengeConsentCommandValidator()
    {
        RuleFor(c => c.CallerId).NotEmpty();
        RuleFor(c => c.ChildId).NotEmpty();
        RuleFor(c => c.ChildId).NotEqual(c => c.CallerId);
    }
}
