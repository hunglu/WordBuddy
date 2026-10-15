using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessons;

public sealed class GetAdminLessonsQueryValidator : AbstractValidator<GetAdminLessonsQuery>
{
    public GetAdminLessonsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
