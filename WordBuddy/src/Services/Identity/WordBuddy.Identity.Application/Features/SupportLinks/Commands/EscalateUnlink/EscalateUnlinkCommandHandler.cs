using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.EscalateUnlink;

/// <summary>Moves a pending request to OverrideRequested. Fails before the wait time, after a decline, or for a non-requester.</summary>
public sealed class EscalateUnlinkCommandHandler : ICommandHandler<EscalateUnlinkCommand>
{
    private readonly ISupportLinkRepository _links;
    private readonly LinkActorResolver _actorResolver;
    private readonly IDistributedCache _cache;
    private readonly SupportLinkOptions _options;
    private readonly TimeProvider _time;
    private readonly IValidator<EscalateUnlinkCommand> _validator;
    private readonly ILogger<EscalateUnlinkCommandHandler> _logger;

    public EscalateUnlinkCommandHandler(
        ISupportLinkRepository links,
        LinkActorResolver actorResolver,
        IDistributedCache cache,
        SupportLinkOptions options,
        TimeProvider time,
        IValidator<EscalateUnlinkCommand> validator,
        ILogger<EscalateUnlinkCommandHandler> logger)
    {
        _links = links;
        _actorResolver = actorResolver;
        _cache = cache;
        _options = options;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(EscalateUnlinkCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("EscalateUnlinkCommand started: ActorId={ActorId}, LinkId={LinkId}", command.ActorId, command.LinkId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("EscalateUnlinkCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("EscalateUnlink.Validation", validation.ToString()));
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
        Result escalate = open.Value.Escalate(command.ActorId, now, _options.UnlinkOverrideWaitDays);
        if (escalate.IsFailure)
        {
            _logger.LogWarning("EscalateUnlinkCommand rejected: LinkId={LinkId}, ErrorCode={ErrorCode}", link.Id, escalate.Error.Code);
            return escalate;
        }

        await _links.AddAuditEntryAsync(
            new SupportLinkAuditEntry(Guid.NewGuid(), link.Id, command.ActorId, SupportLinkAuditAction.UnlinkEscalated, now), ct);

        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            return save;
        }

        await SupportLinkCache.InvalidateAsync(_cache, ct, link.LearnerId, link.SupporterId, actor.Value.LearnerPrimarySupporterId);
        _logger.LogInformation("EscalateUnlinkCommand succeeded: LinkId={LinkId}, UnlinkRequestId={UnlinkRequestId}", link.Id, open.Value.Id);
        return Result.Success();
    }
}
