using FluentValidation;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.PublishLesson;

public sealed class PublishLessonCommandValidator : AbstractValidator<PublishLessonCommand>
{
    public PublishLessonCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
    }
}
