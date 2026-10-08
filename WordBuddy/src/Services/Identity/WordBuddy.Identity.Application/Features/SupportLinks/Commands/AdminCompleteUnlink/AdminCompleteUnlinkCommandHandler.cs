using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminCompleteUnlink;

/// <summary>Revokes the link of an escalated request, publishes <c>SupportLinkRevoked</c>, writes an audit row with the reason.</summary>
public sealed class AdminCompleteUnlinkCommandHandler : ICommandHandler<AdminCompleteUnlinkCommand>
{
    private readonly ISupportLinkRepository _links;
    private readonly ISupportLinkEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<AdminCompleteUnlinkCommand> _validator;
    private readonly ILogger<AdminCompleteUnlinkCommandHandler> _logger;

    public AdminCompleteUnlinkCommandHandler(
        ISupportLinkRepository links,
        ISupportLinkEventPublisher events,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<AdminCompleteUnlinkCommand> validator,
        ILogger<AdminCompleteUnlinkCommandHandler> logger)
    {
        _links = links;
        _events = events;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(AdminCompleteUnlinkCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AdminCompleteUnlinkCommand started: AdminId={AdminId}, UnlinkRequestId={UnlinkRequestId}",
            command.AdminId, command.UnlinkRequestId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AdminCompleteUnlinkCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("AdminCompleteUnlink.Validation", validation.ToString()));
        }

        Result<UnlinkRequest> found = await _links.GetUnlinkRequestTrackedAsync(command.UnlinkRequestId, ct);
        if (found.IsFailure)
        {
            return found;
        }

        UnlinkRequest request = found.Value;
        Result<SupportLink> linkResult = await _links.GetLinkTrackedAsync(request.LinkId, ct);
        if (linkResult.IsFailure)
        {
            return linkResult;
        }

        SupportLink link = linkResult.Value;
        DateTime now = _time.GetUtcNow().UtcDateTime;

        Result complete = request.CompleteByAdmin(command.AdminId, now);
        if (complete.IsFailure)
        {
            _logger.LogWarning("AdminCompleteUnlinkCommand rejected: ErrorCode={ErrorCode}", complete.Error.Code);
            return complete;
        }

        Result revoke = link.Revoke(now);
        if (revoke.IsFailure)
        {
            _logger.LogWarning("AdminCompleteUnlinkCommand rejected: LinkId={LinkId}, ErrorCode={ErrorCode}", link.Id, revoke.Error.Code);
            return revoke;
        }

        await _events.PublishRevokedAsync(link, now, ct);
        await _links.AddAuditEntryAsync(
            new SupportLinkAuditEntry(Guid.NewGuid(), link.Id, command.AdminId, SupportLinkAuditAction.AdminUnlinkCompleted, now, command.Reason.Trim()),
            ct);

        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return save;
        }

        Guid? primaryId = await _links.GetActivePrimarySupporterIdAsync(link.LearnerId, ct);
        await SupportLinkCache.InvalidateAsync(_cache, ct, link.LearnerId, link.SupporterId, primaryId);
        _logger.LogInformation("AdminCompleteUnlinkCommand succeeded: LinkId={LinkId}", link.Id);
        return Result.Success();
    }
}
