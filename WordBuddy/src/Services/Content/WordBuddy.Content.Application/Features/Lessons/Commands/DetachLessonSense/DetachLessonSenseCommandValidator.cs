using FluentValidation;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.DetachLessonSense;

public sealed class DetachLessonSenseCommandValidator : AbstractValidator<DetachLessonSenseCommand>
{
    public DetachLessonSenseCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
        RuleFor(c => c.SenseId).NotEmpty();
    }
}
