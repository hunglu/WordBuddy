using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Users.Queries.GetUsersForAdmin;

public sealed class GetUsersForAdminQueryValidator : AbstractValidator<GetUsersForAdminQuery>
{
    /// <summary>Maximum search text length.</summary>
    public const int SearchMaxLength = 100;

    public GetUsersForAdminQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
        RuleFor(q => q.Search).MaximumLength(SearchMaxLength);
    }
}
