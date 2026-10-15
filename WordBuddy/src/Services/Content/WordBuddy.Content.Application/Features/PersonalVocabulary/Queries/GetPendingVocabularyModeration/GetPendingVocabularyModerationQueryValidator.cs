using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetPendingVocabularyModeration;

public sealed class GetPendingVocabularyModerationQueryValidator : AbstractValidator<GetPendingVocabularyModerationQuery>
{
    public GetPendingVocabularyModerationQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
