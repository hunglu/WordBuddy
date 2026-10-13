using FluentValidation;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.SearchLessonSenses;

public sealed class SearchLessonSensesQueryValidator : AbstractValidator<SearchLessonSensesQuery>
{
    public SearchLessonSensesQueryValidator()
    {
        RuleFor(q => q.Search).MaximumLength(100);
    }
}
