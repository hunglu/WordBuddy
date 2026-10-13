using FluentValidation;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.ReorderLessonSenses;

public sealed class ReorderLessonSensesCommandValidator : AbstractValidator<ReorderLessonSensesCommand>
{
    public ReorderLessonSensesCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
        RuleFor(c => c.SenseIds).NotNull();
        RuleForEach(c => c.SenseIds).NotEmpty();
    }
}
