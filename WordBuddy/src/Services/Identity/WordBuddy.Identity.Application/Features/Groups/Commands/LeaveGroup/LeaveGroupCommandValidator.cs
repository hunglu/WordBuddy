using FluentValidation;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.LeaveGroup;

public sealed class LeaveGroupCommandValidator : AbstractValidator<LeaveGroupCommand>
{
    public LeaveGroupCommandValidator()
    {
        RuleFor(c => c.GroupId).NotEmpty();
        RuleFor(c => c.LearnerId).NotEmpty();
    }
}
