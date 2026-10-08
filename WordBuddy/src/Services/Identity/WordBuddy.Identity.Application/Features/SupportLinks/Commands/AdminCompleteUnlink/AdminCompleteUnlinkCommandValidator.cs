using FluentValidation;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminCompleteUnlink;

public sealed class AdminCompleteUnlinkCommandValidator : AbstractValidator<AdminCompleteUnlinkCommand>
{
    public AdminCompleteUnlinkCommandValidator()
    {
        RuleFor(c => c.AdminId).NotEmpty();
        RuleFor(c => c.UnlinkRequestId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(SupportLinkAuditEntry.ReasonMaxLength);
    }
}
