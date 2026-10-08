using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelInvitation;

public sealed class CancelInvitationCommandHandler : ICommandHandler<CancelInvitationCommand>
{
    private readonly ISupportLinkRepository _links;
    private readonly IDistributedCache _cache;
    private readonly IValidator<CancelInvitationCommand> _validator;
    private readonly ILogger<CancelInvitationCommandHandler> _logger;

    public CancelInvitationCommandHandler(
        ISupportLinkRepository links,
        IDistributedCache cache,
        IValidator<CancelInvitationCommand> validator,
        ILogger<CancelInvitationCommandHandler> logger)
    {
        _links = links;
        _cache = cache;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(CancelInvitationCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "CancelInvitationCommand started: ActorId={ActorId}, InvitationId={InvitationId}", command.ActorId, command.InvitationId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CancelInvitationCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("CancelInvitation.Validation", validation.ToString()));
        }

        Result<SupportLinkInvitation> found = await _links.GetInvitationTrackedAsync(command.InvitationId, ct);
        if (found.IsFailure)
        {
            return found;
        }

        Result cancel = found.Value.Cancel(command.ActorId);
        if (cancel.IsFailure)
        {
            _logger.LogWarning("CancelInvitationCommand rejected: ErrorCode={ErrorCode}", cancel.Error.Code);
            return cancel;
        }

        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return save;
        }

        await SupportLinkCache.InvalidateAsync(_cache, ct, command.ActorId);
        _logger.LogInformation("CancelInvitationCommand succeeded: InvitationId={InvitationId}", command.InvitationId);
        return Result.Success();
    }
}
