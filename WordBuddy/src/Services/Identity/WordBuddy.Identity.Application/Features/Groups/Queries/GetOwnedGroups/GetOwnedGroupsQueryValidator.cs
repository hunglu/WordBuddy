using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetOwnedGroups;

public sealed class GetOwnedGroupsQueryValidator : AbstractValidator<GetOwnedGroupsQuery>
{
    public GetOwnedGroupsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
