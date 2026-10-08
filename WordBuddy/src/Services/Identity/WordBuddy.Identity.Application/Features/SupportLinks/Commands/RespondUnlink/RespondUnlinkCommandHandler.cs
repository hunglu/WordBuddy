using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondUnlink;

/// <summary>Confirm revokes the link and publishes <c>SupportLinkRevoked</c>; decline closes the request.</summary>
public sealed class RespondUnlinkCommandHandler : ICommandHandler<RespondUnlinkCommand>
{
    private readonly ISupportLinkRepository _links;
    private readonly LinkActorResolver _actorResolver;
    private readonly ISupportLinkEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<RespondUnlinkCommand> _validator;
    private readonly ILogger<RespondUnlinkCommandHandler> _logger;

    public RespondUnlinkCommandHandler(
        ISupportLinkRepository links,
        LinkActorResolver actorResolver,
        ISupportLinkEventPublisher events,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<RespondUnlinkCommand> validator,
        ILogger<RespondUnlinkCommandHandler> logger)
    {
        _links = links;
        _actorResolver = actorResolver;
        _events = events;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RespondUnlinkCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RespondUnlinkCommand started: ActorId={ActorId}, LinkId={LinkId}, Confirm={Confirm}",
            command.ActorId, command.LinkId, command.Confirm);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RespondUnlinkCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RespondUnlink.Validation", validation.ToString()));
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
            _logger.LogWarning("RespondUnlinkCommand rejected: LinkId={LinkId}, ErrorCode={ErrorCode}", link.Id, actor.Error.Code);
            return actor;
        }

        Result<UnlinkRequest> open = await _links.GetOpenUnlinkRequestTrackedAsync(link.Id, ct);
        if (open.IsFailure)
        {
            return open;
        }

        DateTime now = _time.GetUtcNow().UtcDateTime;
        UnlinkRequest request = open.Value;
        Result respond = command.Confirm
            ? request.Confirm(command.ActorId, actor.Value.Side, now)
            : request.Decline(command.ActorId, actor.Value.Side, now);
        if (respond.IsFailure)
        {
            _logger.LogWarning("RespondUnlinkCommand rejected: LinkId={LinkId}, ErrorCode={ErrorCode}", link.Id, respond.Error.Code);
            return respond;
        }

        if (command.Confirm)
        {
            Result revoke = link.Revoke(now);
            if (revoke.IsFailure)
            {
                return revoke;
            }

            await _events.PublishRevokedAsync(link, now, ct);
        }

        SupportLinkAuditAction action = command.Confirm ? SupportLinkAuditAction.UnlinkConfirmed : SupportLinkAuditAction.UnlinkDeclined;
        await _links.AddAuditEntryAsync(new SupportLinkAuditEntry(Guid.NewGuid(), link.Id, command.ActorId, action, now), ct);

        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("RespondUnlinkCommand failed to persist: {ErrorCode}", save.Error.Code);
            return save;
        }

        await SupportLinkCache.InvalidateAsync(_cache, ct, link.LearnerId, link.SupporterId, actor.Value.LearnerPrimarySupporterId);
        _logger.LogInformation(
            "RespondUnlinkCommand succeeded: LinkId={LinkId}, RequestStatus={RequestStatus}", link.Id, request.Status);
        return Result.Success();
    }
}
