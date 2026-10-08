using FluentValidation;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondToPendingSupporter;

public sealed class RespondToPendingSupporterCommandValidator : AbstractValidator<RespondToPendingSupporterCommand>
{
    public RespondToPendingSupporterCommandValidator()
    {
        RuleFor(c => c.ActorId).NotEmpty();
        RuleFor(c => c.LinkId).NotEmpty();
    }
}
