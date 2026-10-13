using FluentValidation;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.UnpublishLesson;

public sealed class UnpublishLessonCommandValidator : AbstractValidator<UnpublishLessonCommand>
{
    public UnpublishLessonCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
    }
}
