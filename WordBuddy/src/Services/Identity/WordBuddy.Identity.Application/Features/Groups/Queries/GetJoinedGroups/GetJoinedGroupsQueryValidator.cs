using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetJoinedGroups;

public sealed class GetJoinedGroupsQueryValidator : AbstractValidator<GetJoinedGroupsQuery>
{
    public GetJoinedGroupsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
