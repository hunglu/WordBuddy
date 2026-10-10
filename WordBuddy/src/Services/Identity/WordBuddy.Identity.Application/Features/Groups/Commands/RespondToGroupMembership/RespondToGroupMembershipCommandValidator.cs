using FluentValidation;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.RespondToGroupMembership;

public sealed class RespondToGroupMembershipCommandValidator : AbstractValidator<RespondToGroupMembershipCommand>
{
    public RespondToGroupMembershipCommandValidator()
    {
        RuleFor(c => c.MemberId).NotEmpty();
        RuleFor(c => c.CallerId).NotEmpty();
    }
}
