using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Groups.Queries.GetGroupWordAssignments;

public sealed class GetGroupWordAssignmentsQueryValidator : AbstractValidator<GetGroupWordAssignmentsQuery>
{
    public GetGroupWordAssignmentsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
