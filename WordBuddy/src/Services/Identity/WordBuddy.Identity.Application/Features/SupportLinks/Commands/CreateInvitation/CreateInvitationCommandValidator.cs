using FluentValidation;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.CreateInvitation;

public sealed class CreateInvitationCommandValidator : AbstractValidator<CreateInvitationCommand>
{
    public CreateInvitationCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.InviteAs).IsInEnum();
        RuleFor(c => c.Relationship).IsInEnum().When(c => c.Relationship.HasValue);
    }
}
