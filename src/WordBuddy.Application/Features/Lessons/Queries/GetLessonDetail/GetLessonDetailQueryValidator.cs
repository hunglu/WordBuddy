using FluentValidation;

namespace WordBuddy.Application.Features.Lessons.Queries.GetLessonDetail;

internal sealed class GetLessonDetailQueryValidator : AbstractValidator<GetLessonDetailQuery>
{
    public GetLessonDetailQueryValidator()
    {
        RuleFor(x => x.LessonId).NotEmpty();
    }
}
