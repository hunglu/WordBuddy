using FluentValidation;
using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.Features.Challenges.Commands.ApplyLearnerProfileEvent;

public sealed class ApplyLearnerProfileEventCommandValidator : AbstractValidator<ApplyLearnerProfileEventCommand>
{
    public ApplyLearnerProfileEventCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.AgeGroup)
            .Must(a => Enum.TryParse(a, ignoreCase: true, out AgeGroup parsed) && Enum.IsDefined(parsed))
            .WithMessage("The age group must be Child or Adult.");
        RuleFor(c => c.Alias).MaximumLength(20);
        RuleFor(c => c.AvatarId).MaximumLength(30);
        RuleFor(c => c.DisplayName).MaximumLength(100);
        RuleFor(c => c.TimeZoneId).MaximumLength(64);
        RuleFor(c => c.OccurredAtUtc).NotEmpty();
    }
}
