using FluentValidation;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelUnlink;

public sealed class CancelUnlinkCommandValidator : AbstractValidator<CancelUnlinkCommand>
{
    public CancelUnlinkCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.LinkId).NotEmpty();
    }
}
