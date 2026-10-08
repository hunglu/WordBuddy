using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondToPendingSupporter;

/// <summary>Only the active Primary of the child may approve (publishes <c>SupportLinkActivated</c>) or reject.</summary>
public sealed class RespondToPendingSupporterCommandHandler : ICommandHandler<RespondToPendingSupporterCommand>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly ISupportLinkEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<RespondToPendingSupporterCommand> _validator;
    private readonly ILogger<RespondToPendingSupporterCommandHandler> _logger;

    public RespondToPendingSupporterCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        ISupportLinkEventPublisher events,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<RespondToPendingSupporterCommand> validator,
        ILogger<RespondToPendingSupporterCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _events = events;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RespondToPendingSupporterCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RespondToPendingSupporterCommand started: ActorId={ActorId}, LinkId={LinkId}, Approve={Approve}",
            command.ActorId, command.LinkId, command.Approve);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RespondToPendingSupporterCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RespondToPendingSupporter.Validation", validation.ToString()));
        }

        Result<SupportLink> found = await _links.GetLinkTrackedAsync(command.LinkId, ct);
        if (found.IsFailure)
        {
            return found;
        }

        Result<User> actor = await _users.GetByIdAsync(command.ActorId, ct);
        if (actor.IsFailure)
        {
            return actor;
        }

        SupportLink link = found.Value;
        Guid? primaryId = await _links.GetActivePrimarySupporterIdAsync(link.LearnerId, ct);

        Result allowed = SupportLinkPolicy.CanRespondToPending(link, new LinkParty(actor.Value.Id, actor.Value.AgeGroup), primaryId);
        if (allowed.IsFailure)
        {
            _logger.LogWarning(
                "RespondToPendingSupporterCommand rejected: LinkId={LinkId}, ErrorCode={ErrorCode}", link.Id, allowed.Error.Code);
            return allowed;
        }

        DateTime now = _time.GetUtcNow().UtcDateTime;
        Result change = command.Approve ? link.ApproveByPrimary(now) : link.RejectByPrimary(now);
        if (change.IsFailure)
        {
            return change;
        }

        SupportLinkAuditAction action = command.Approve ? SupportLinkAuditAction.PrimaryApproved : SupportLinkAuditAction.PrimaryRejected;
        await _links.AddAuditEntryAsync(new SupportLinkAuditEntry(Guid.NewGuid(), link.Id, command.ActorId, action, now), ct);

        if (command.Approve)
        {
            await _events.PublishActivatedAsync(link, now, ct);
        }

        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("RespondToPendingSupporterCommand failed to persist: {ErrorCode}", save.Error.Code);
            return save;
        }

        await SupportLinkCache.InvalidateAsync(_cache, ct, link.LearnerId, link.SupporterId, primaryId);
        _logger.LogInformation(
            "RespondToPendingSupporterCommand succeeded: LinkId={LinkId}, Status={Status}", link.Id, link.Status);
        return Result.Success();
    }
}
