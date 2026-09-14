using FluentValidation;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.CreateLesson;

public sealed class CreateLessonCommandValidator : AbstractValidator<CreateLessonCommand>
{
    public CreateLessonCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.Level).IsInEnum();
        RuleFor(c => c.TargetAgeGroup).IsInEnum();
    }
}
