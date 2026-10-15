using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetLearnerSupportLinksForAdmin;

public sealed class GetLearnerSupportLinksForAdminQueryValidator : AbstractValidator<GetLearnerSupportLinksForAdminQuery>
{
    public GetLearnerSupportLinksForAdminQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
        RuleFor(q => q.LearnerId).NotEqual(Guid.Empty).WithMessage("LearnerId is required.");
    }
}
