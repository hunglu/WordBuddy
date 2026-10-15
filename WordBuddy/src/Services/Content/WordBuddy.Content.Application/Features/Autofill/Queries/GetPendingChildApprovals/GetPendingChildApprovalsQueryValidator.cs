using FluentValidation;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Autofill.Queries.GetPendingChildApprovals;

public sealed class GetPendingChildApprovalsQueryValidator : AbstractValidator<GetPendingChildApprovalsQuery>
{
    public GetPendingChildApprovalsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
        RuleFor(q => q.LearnerId).NotEqual(Guid.Empty).WithMessage("A learner id is required.");
    }
}
