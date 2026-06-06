using FluentValidation;

namespace WordBuddy.Application.Features.Lessons.Queries.GetLessons;

internal sealed class GetLessonsQueryValidator : AbstractValidator<GetLessonsQuery>
{
    public GetLessonsQueryValidator()
    {
        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.Level).IsInEnum().When(x => x.Level.HasValue);
        RuleFor(x => x.AgeGroup).IsInEnum().When(x => x.AgeGroup.HasValue);
    }
}
