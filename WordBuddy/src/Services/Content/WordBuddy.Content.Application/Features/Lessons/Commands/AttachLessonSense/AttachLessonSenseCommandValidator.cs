using FluentValidation;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.AttachLessonSense;

public sealed class AttachLessonSenseCommandValidator : AbstractValidator<AttachLessonSenseCommand>
{
    public AttachLessonSenseCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
        RuleFor(c => c.SenseId).NotEmpty();
    }
}
