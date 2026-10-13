using FluentValidation;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.CurrentPassword).NotEmpty();

        // Same minimum as registration.
        RuleFor(c => c.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(c => c.NewPassword)
            .NotEqual(c => c.CurrentPassword)
            .WithMessage("The new password must differ from the current password.")
            .When(c => !string.IsNullOrEmpty(c.NewPassword));
    }
}
