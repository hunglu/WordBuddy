using FluentValidation;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondUnlink;

public sealed class RespondUnlinkCommandValidator : AbstractValidator<RespondUnlinkCommand>
{
    public RespondUnlinkCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.LinkId).NotEmpty();
    }
}
