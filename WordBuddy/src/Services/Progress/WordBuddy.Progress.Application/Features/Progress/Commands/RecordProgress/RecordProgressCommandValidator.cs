using FluentValidation;

namespace WordBuddy.Progress.Application.Features.Progress.Commands.RecordProgress;

public sealed class RecordProgressCommandValidator : AbstractValidator<RecordProgressCommand>
{
    public RecordProgressCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.LessonId).NotEmpty();
        RuleFor(c => c.ScorePercent).InclusiveBetween(0, 100).When(c => c.ScorePercent.HasValue);
    }
}
