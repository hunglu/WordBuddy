using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.RequestUnlink;

/// <summary>Opens an unlink request. Rejected for a child caller, for the Primary link, and when one is already open.</summary>
public sealed class RequestUnlinkCommandHandler : ICommandHandler<RequestUnlinkCommand>
{
    private readonly ISupportLinkRepository _links;
    private readonly LinkActorResolver _actorResolver;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<RequestUnlinkCommand> _validator;
    private readonly ILogger<RequestUnlinkCommandHandler> _logger;

    public RequestUnlinkCommandHandler(
        ISupportLinkRepository links,
        LinkActorResolver actorResolver,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<RequestUnlinkCommand> validator,
        ILogger<RequestUnlinkCommandHandler> logger)
    {
        _links = links;
        _actorResolver = actorResolver;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RequestUnlinkCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RequestUnlinkCommand started: ActorId={ActorId}, LinkId={LinkId}", command.ActorId, command.LinkId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RequestUnlinkCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RequestUnlink.Validation", validation.ToString()));
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
            _logger.LogWarning("RequestUnlinkCommand rejected: LinkId={LinkId}, ErrorCode={ErrorCode}", link.Id, actor.Error.Code);
            return actor;
        }

        Result allowed = SupportLinkPolicy.CanRequestUnlink(link);
        if (allowed.IsFailure)
        {
            _logger.LogWarning("RequestUnlinkCommand rejected: LinkId={LinkId}, ErrorCode={ErrorCode}", link.Id, allowed.Error.Code);
            return allowed;
        }

        Result<UnlinkRequest> open = await _links.GetOpenUnlinkRequestTrackedAsync(link.Id, ct);
        if (open.IsSuccess)
        {
            return Result.Failure(SupportLinkErrors.UnlinkAlreadyOpen);
        }

        DateTime now = _time.GetUtcNow().UtcDateTime;
        UnlinkRequest request = UnlinkRequest.Create(Guid.NewGuid(), link.Id, command.ActorId, actor.Value.Side, now);
        await _links.AddUnlinkRequestAsync(request, ct);
        await _links.AddAuditEntryAsync(
            new SupportLinkAuditEntry(Guid.NewGuid(), link.Id, command.ActorId, SupportLinkAuditAction.UnlinkRequested, now), ct);

        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("RequestUnlinkCommand failed to persist: {ErrorCode}", save.Error.Code);
            return save;
        }

        await SupportLinkCache.InvalidateAsync(_cache, ct, link.LearnerId, link.SupporterId, actor.Value.LearnerPrimarySupporterId);
        _logger.LogInformation(
            "RequestUnlinkCommand succeeded: LinkId={LinkId}, UnlinkRequestId={UnlinkRequestId}, Side={Side}",
            link.Id, request.Id, request.RequestedBySide);
        return Result.Success();
    }
}
