using FluentValidation;

namespace WordBuddy.Progress.Application.Features.VocabularyRecall.Commands.RemapVocabularyWordIds;

public sealed class RemapVocabularyWordIdsCommandValidator : AbstractValidator<RemapVocabularyWordIdsCommand>
{
    /// <summary>Content publishes in batches of 500; leave headroom.</summary>
    public const int MaxRemaps = 1000;

    public RemapVocabularyWordIdsCommandValidator()
    {
        RuleFor(c => c.Remaps)
            .NotNull()
            .Must(r => r.Count <= MaxRemaps).WithMessage($"A remap batch cannot contain more than {MaxRemaps} pairs.")
            .Must(r => r.Select(p => p.OldId).Distinct().Count() == r.Count).WithMessage("Each old id may appear only once.");

        RuleForEach(c => c.Remaps).ChildRules(remap =>
        {
            remap.RuleFor(p => p.OldId).NotEmpty();
            remap.RuleFor(p => p.NewId).NotEmpty();
            remap.RuleFor(p => p).Must(p => p.OldId != p.NewId).WithMessage("Old and new id must differ.");
        });
    }
}
