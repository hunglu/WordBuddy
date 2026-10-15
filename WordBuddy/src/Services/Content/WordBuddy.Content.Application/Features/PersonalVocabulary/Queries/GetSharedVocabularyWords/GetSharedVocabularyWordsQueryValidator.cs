using FluentValidation;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSharedVocabularyWords;

public sealed class GetSharedVocabularyWordsQueryValidator : AbstractValidator<GetSharedVocabularyWordsQuery>
{
    public GetSharedVocabularyWordsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
        RuleFor(q => q.Search).MaximumLength(Sense.WordMaxLength);
    }
}
