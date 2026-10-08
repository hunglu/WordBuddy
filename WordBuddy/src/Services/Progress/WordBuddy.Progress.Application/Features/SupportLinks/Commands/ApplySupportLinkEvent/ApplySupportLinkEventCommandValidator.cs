using FluentValidation;

namespace WordBuddy.Progress.Application.Features.SupportLinks.Commands.ApplySupportLinkEvent;

public sealed class ApplySupportLinkEventCommandValidator : AbstractValidator<ApplySupportLinkEventCommand>
{
    public ApplySupportLinkEventCommandValidator()
    {
        RuleFor(c => c.LinkId).NotEmpty();
        RuleFor(c => c.LearnerId).NotEmpty();
        RuleFor(c => c.SupporterId).NotEmpty();
        RuleFor(c => c.OccurredAtUtc).NotEmpty();
    }
}
