using FluentValidation;

namespace WordBuddy.Application.Features.Progress.Commands.RecordProgress;

internal sealed class RecordProgressCommandValidator : AbstractValidator<RecordProgressCommand>
{
    public RecordProgressCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.LessonId).NotEmpty();
        RuleFor(x => x.ScorePercent)
            .InclusiveBetween(0, 100)
            .When(x => x.ScorePercent.HasValue);
    }
}
