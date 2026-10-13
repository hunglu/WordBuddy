using FluentValidation;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.DeleteLesson;

public sealed class DeleteLessonCommandValidator : AbstractValidator<DeleteLessonCommand>
{
    public DeleteLessonCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
    }
}
