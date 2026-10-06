using FluentValidation;

namespace WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordRemoved;

public sealed class RecordLearnerWordRemovedCommandValidator : AbstractValidator<RecordLearnerWordRemovedCommand>
{
    public RecordLearnerWordRemovedCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.SenseId).NotEmpty();
        RuleFor(c => c.RemovedAtUtc).NotEmpty();
    }
}
