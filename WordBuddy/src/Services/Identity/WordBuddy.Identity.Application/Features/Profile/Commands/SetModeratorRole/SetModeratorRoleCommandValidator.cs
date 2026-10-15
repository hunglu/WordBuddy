using FluentValidation;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.SetModeratorRole;

public sealed class SetModeratorRoleCommandValidator : AbstractValidator<SetModeratorRoleCommand>
{
    public SetModeratorRoleCommandValidator()
    {
        RuleFor(c => c.AdminId).NotEmpty();
        RuleFor(c => c.TargetUserId).NotEmpty();
    }
}
