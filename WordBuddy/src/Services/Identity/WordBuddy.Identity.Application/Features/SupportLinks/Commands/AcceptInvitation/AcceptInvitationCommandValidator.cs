using FluentValidation;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AcceptInvitation;

public sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c)
            .Must(c => string.IsNullOrWhiteSpace(c.Code) != string.IsNullOrWhiteSpace(c.Token))
            .WithMessage("Provide either a code or a token.");
        RuleFor(c => c.Code!).Length(8, 9).When(c => !string.IsNullOrWhiteSpace(c.Code));
        RuleFor(c => c.Token!).MaximumLength(100).When(c => !string.IsNullOrWhiteSpace(c.Token));
    }
}
