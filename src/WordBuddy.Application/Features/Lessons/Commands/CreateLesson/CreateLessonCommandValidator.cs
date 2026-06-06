using FluentValidation;

namespace WordBuddy.Application.Features.Lessons.Commands.CreateLesson;

internal sealed class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Level).IsInEnum();
        RuleFor(x => x.TargetAgeGroup).IsInEnum();
        RuleFor(x => x.OrderIndex).GreaterThanOrEqualTo(0);
    }
}
