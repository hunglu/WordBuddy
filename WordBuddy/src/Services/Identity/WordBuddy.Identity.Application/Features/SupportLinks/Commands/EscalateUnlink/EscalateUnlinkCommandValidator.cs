using FluentValidation;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.EscalateUnlink;

public sealed class EscalateUnlinkCommandValidator : AbstractValidator<EscalateUnlinkCommand>
{
    public EscalateUnlinkCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.LinkId).NotEmpty();
    }
}
