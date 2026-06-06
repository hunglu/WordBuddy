using FluentValidation;

namespace WordBuddy.Application.Features.Progress.Queries.GetUserProgress;

internal sealed class GetUserProgressQueryValidator : AbstractValidator<GetUserProgressQuery>
{
    public GetUserProgressQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
