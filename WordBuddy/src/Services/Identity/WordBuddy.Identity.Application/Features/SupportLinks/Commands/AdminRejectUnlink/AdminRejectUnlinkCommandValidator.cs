using FluentValidation;
using WordBuddy.Identity.Domain.SupportLinks;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminRejectUnlink;

public sealed class AdminRejectUnlinkCommandValidator : AbstractValidator<AdminRejectUnlinkCommand>
{
    public AdminRejectUnlinkCommandValidator()
    {
        RuleFor(c => c.AdminId).NotEmpty();
        RuleFor(c => c.UnlinkRequestId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty().MaximumLength(SupportLinkAuditEntry.ReasonMaxLength);
    }
}
