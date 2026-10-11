using FluentValidation;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.SetGuardianRole;

public sealed class SetGuardianRoleCommandValidator : AbstractValidator<SetGuardianRoleCommand>
{
    public SetGuardianRoleCommandValidator()
    {
        RuleFor(c => c.AdminId).NotEmpty();
        RuleFor(c => c.TargetUserId).NotEmpty();
    }
}
