using FluentValidation;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.RemoveGroupMember;

public sealed class RemoveGroupMemberCommandValidator : AbstractValidator<RemoveGroupMemberCommand>
{
    public RemoveGroupMemberCommandValidator()
    {
        RuleFor(c => c.GroupId).NotEmpty();
        RuleFor(c => c.CallerId).NotEmpty();
        RuleFor(c => c.LearnerId).NotEmpty();
    }
}
