using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;

public sealed class GetUnlinkRequestsForAdminQueryValidator : AbstractValidator<GetUnlinkRequestsForAdminQuery>
{
    public GetUnlinkRequestsForAdminQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}
