using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetGroupMembers;

public sealed class GetGroupMembersQueryValidator : AbstractValidator<GetGroupMembersQuery>
{
    public GetGroupMembersQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
