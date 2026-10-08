using FluentValidation;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelInvitation;

public sealed class CancelInvitationCommandValidator : AbstractValidator<CancelInvitationCommand>
{
    public CancelInvitationCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.InvitationId).NotEmpty();
    }
}
