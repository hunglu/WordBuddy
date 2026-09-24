using FluentValidation;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SubmitVocabularyRecallCheck;

public sealed class SubmitVocabularyRecallCheckCommandValidator : AbstractValidator<SubmitVocabularyRecallCheckCommand>
{
    public SubmitVocabularyRecallCheckCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Results)
            .NotEmpty()
            .Must(r => r.Count <= 50).WithMessage("A recall-check submission cannot contain more than 50 results.");

        RuleForEach(c => c.Results).ChildRules(result =>
        {
            result.RuleFor(r => r.VocabularyWordId).NotEmpty();
            result.RuleFor(r => r.Word).NotEmpty().MaximumLength(200);
        });
    }
}
