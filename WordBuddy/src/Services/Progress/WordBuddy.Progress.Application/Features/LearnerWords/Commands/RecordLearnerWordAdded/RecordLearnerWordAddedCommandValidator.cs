using FluentValidation;

namespace WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordAdded;

public sealed class RecordLearnerWordAddedCommandValidator : AbstractValidator<RecordLearnerWordAddedCommand>
{
    public RecordLearnerWordAddedCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.SenseId).NotEmpty();
        RuleFor(c => c.AddedBy).NotEmpty();
        RuleFor(c => c.AddedAtUtc).NotEmpty();
    }
}
