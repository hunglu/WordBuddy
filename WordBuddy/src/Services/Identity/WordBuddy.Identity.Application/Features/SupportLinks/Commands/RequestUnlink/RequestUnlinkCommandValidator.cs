using FluentValidation;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.RequestUnlink;

public sealed class RequestUnlinkCommandValidator : AbstractValidator<RequestUnlinkCommand>
{
    public RequestUnlinkCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.LinkId).NotEmpty();
    }
}
