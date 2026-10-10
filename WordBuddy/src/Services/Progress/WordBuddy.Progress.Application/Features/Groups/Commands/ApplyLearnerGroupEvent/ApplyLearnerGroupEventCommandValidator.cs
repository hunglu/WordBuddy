using FluentValidation;

namespace WordBuddy.Progress.Application.Features.Groups.Commands.ApplyLearnerGroupEvent;

public sealed class ApplyLearnerGroupEventCommandValidator : AbstractValidator<ApplyLearnerGroupEventCommand>
{
    public ApplyLearnerGroupEventCommandValidator()
    {
        RuleFor(c => c.GroupId).NotEmpty();
        RuleFor(c => c.OwnerId).NotEmpty();
        RuleFor(c => c.LearnerId).NotEqual(Guid.Empty);
        RuleFor(c => c.OccurredAtUtc).NotEmpty();
        RuleFor(c => c.IsActive).Equal(false).When(c => c.LearnerId is null)
            .WithMessage("A whole-group event can only deactivate.");
    }
}
