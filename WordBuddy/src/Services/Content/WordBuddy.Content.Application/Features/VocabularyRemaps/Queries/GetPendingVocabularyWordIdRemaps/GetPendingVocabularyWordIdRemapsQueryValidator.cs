using FluentValidation;

namespace WordBuddy.Content.Application.Features.VocabularyRemaps.Queries.GetPendingVocabularyWordIdRemaps;

public sealed class GetPendingVocabularyWordIdRemapsQueryValidator : AbstractValidator<GetPendingVocabularyWordIdRemapsQuery>
{
    /// <summary>Largest batch a caller may request.</summary>
    public const int MaxLimit = 500;

    public GetPendingVocabularyWordIdRemapsQueryValidator()
    {
        RuleFor(q => q.Limit).InclusiveBetween(1, MaxLimit);
    }
}
