using FluentValidation;

namespace WordBuddy.Content.Application.Features.VocabularyRemaps.Commands.AcknowledgeVocabularyWordIdRemaps;

public sealed class AcknowledgeVocabularyWordIdRemapsCommandValidator : AbstractValidator<AcknowledgeVocabularyWordIdRemapsCommand>
{
    /// <summary>Largest acknowledgement batch — matches the largest pending batch.</summary>
    public const int MaxIds = 500;

    public AcknowledgeVocabularyWordIdRemapsCommandValidator()
    {
        RuleFor(c => c.OldIds)
            .NotEmpty()
            .Must(ids => ids is null || ids.Count <= MaxIds).WithMessage($"At most {MaxIds} ids may be acknowledged at once.");

        RuleForEach(c => c.OldIds).NotEmpty();
    }
}
