using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetPendingGroupApprovals;

public sealed class GetPendingGroupApprovalsQueryValidator : AbstractValidator<GetPendingGroupApprovalsQuery>
{
    public GetPendingGroupApprovalsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
