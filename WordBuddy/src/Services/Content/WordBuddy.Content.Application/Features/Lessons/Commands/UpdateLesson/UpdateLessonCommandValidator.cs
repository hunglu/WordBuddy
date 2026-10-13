using FluentValidation;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.UpdateLesson;

public sealed class UpdateLessonCommandValidator : AbstractValidator<UpdateLessonCommand>
{
    public UpdateLessonCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Level).IsInEnum();
        RuleFor(c => c.TargetAgeGroup).IsInEnum();
    }
}
