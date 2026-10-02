using FluentValidation;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.SyncVocabularyWordIdRemaps;

public sealed class SyncVocabularyWordIdRemapsCommandValidator : AbstractValidator<SyncVocabularyWordIdRemapsCommand>
{
    /// <summary>Largest batch Content serves per request.</summary>
    public const int MaxBatchSize = 500;

    public SyncVocabularyWordIdRemapsCommandValidator()
    {
        RuleFor(c => c.BatchSize).InclusiveBetween(1, MaxBatchSize);
    }
}
