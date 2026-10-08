using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelUnlink;

public sealed class CancelUnlinkCommandHandler : ICommandHandler<CancelUnlinkCommand>
{
    private readonly ISupportLinkRepository _links;
    private readonly LinkActorResolver _actorResolver;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<CancelUnlinkCommand> _validator;
    private readonly ILogger<CancelUnlinkCommandHandler> _logger;

    public CancelUnlinkCommandHandler(
        ISupportLinkRepository links,
        LinkActorResolver actorResolver,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<CancelUnlinkCommand> validator,
        ILogger<CancelUnlinkCommandHandler> logger)
    {
        _links = links;
        _actorResolver = actorResolver;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(CancelUnlinkCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("CancelUnlinkCommand started: ActorId={ActorId}, LinkId={LinkId}", command.ActorId, command.LinkId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CancelUnlinkCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("CancelUnlink.Validation", validation.ToString()));
        }

        Result<SupportLink> found = await _links.GetLinkTrackedAsync(command.LinkId, ct);
        if (found.IsFailure)
        {
            return found;
        }

        SupportLink link = found.Value;
        Result<ResolvedLinkActor> actor = await _actorResolver.ResolveAsync(command.ActorId, link, ct);
        if (actor.IsFailure)
        {
            return actor;
        }

        Result<UnlinkRequest> open = await _links.GetOpenUnlinkRequestTrackedAsync(link.Id, ct);
        if (open.IsFailure)
        {
            return open;
        }

        DateTime now = _time.GetUtcNow().UtcDateTime;
        Result cancel = open.Value.Cancel(command.ActorId, now);
        if (cancel.IsFailure)
        {
            _logger.LogWarning("CancelUnlinkCommand rejected: LinkId={LinkId}, ErrorCode={ErrorCode}", link.Id, cancel.Error.Code);
            return cancel;
        }

        await _links.AddAuditEntryAsync(
            new SupportLinkAuditEntry(Guid.NewGuid(), link.Id, command.ActorId, SupportLinkAuditAction.UnlinkCancelled, now), ct);

        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return save;
        }

        await SupportLinkCache.InvalidateAsync(_cache, ct, link.LearnerId, link.SupporterId, actor.Value.LearnerPrimarySupporterId);
        _logger.LogInformation("CancelUnlinkCommand succeeded: LinkId={LinkId}", link.Id);
        return Result.Success();
    }
}
