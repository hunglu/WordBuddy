using FluentValidation;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetRandomVocabularyWordsForCheck;

public sealed class GetRandomVocabularyWordsForCheckQueryValidator : AbstractValidator<GetRandomVocabularyWordsForCheckQuery>
{
    public GetRandomVocabularyWordsForCheckQueryValidator()
    {
        RuleFor(q => q.OwnerUserId).NotEmpty();
        RuleFor(q => q.Count).InclusiveBetween(1, 50);
    }
}
