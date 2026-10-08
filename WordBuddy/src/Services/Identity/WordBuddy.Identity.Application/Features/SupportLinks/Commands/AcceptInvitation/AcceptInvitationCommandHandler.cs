using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Application.Settings;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.SupportLinks.Commands.AcceptInvitation;

/// <summary>
/// Accepts an invitation and creates the link via <see cref="SupportLinkPolicy"/>: adult learner or
/// first supporter of a child → Active (publishes <c>SupportLinkActivated</c>); extra supporter of a
/// child → PendingPrimaryApproval. Audit row and event commit in the same save.
/// </summary>
public sealed class AcceptInvitationCommandHandler : ICommandHandler<AcceptInvitationCommand, SupportLinkDto>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly ISupportLinkEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly SupportLinkOptions _options;
    private readonly TimeProvider _time;
    private readonly IValidator<AcceptInvitationCommand> _validator;
    private readonly ILogger<AcceptInvitationCommandHandler> _logger;

    public AcceptInvitationCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        ISupportLinkEventPublisher events,
        IDistributedCache cache,
        SupportLinkOptions options,
        TimeProvider time,
        IValidator<AcceptInvitationCommand> validator,
        ILogger<AcceptInvitationCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _events = events;
        _cache = cache;
        _options = options;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<SupportLinkDto>> HandleAsync(AcceptInvitationCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("AcceptInvitationCommand started: ActorId={ActorId}", command.ActorId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AcceptInvitationCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<SupportLinkDto>(Error.Validation("AcceptInvitation.Validation", validation.ToString()));
        }

        string? codeHash = string.IsNullOrWhiteSpace(command.Code) ? null : SupportLinkInvitation.HashTypedCode(command.Code);
        string? tokenHash = string.IsNullOrWhiteSpace(command.Token) ? null : SupportLinkInvitation.HashToken(command.Token.Trim());

        Result<SupportLinkInvitation> found = await _links.GetInvitationByHashTrackedAsync(codeHash, tokenHash, ct);
        if (found.IsFailure)
        {
            _logger.LogWarning("AcceptInvitationCommand failed: invitation not found, ActorId={ActorId}", command.ActorId);
            return Result.Failure<SupportLinkDto>(found.Error);
        }

        SupportLinkInvitation invitation = found.Value;
        DateTime now = _time.GetUtcNow().UtcDateTime;

        Result canAccept = invitation.CanAccept(command.ActorId, now);
        if (canAccept.IsFailure)
        {
            _logger.LogWarning(
                "AcceptInvitationCommand rejected: InvitationId={InvitationId}, ErrorCode={ErrorCode}",
                invitation.Id, canAccept.Error.Code);
            return Result.Failure<SupportLinkDto>(canAccept.Error);
        }

        IReadOnlyList<User> users = await _users.GetByIdsAsync([invitation.CreatedById, command.ActorId], ct);
        User? creator = users.FirstOrDefault(u => u.Id == invitation.CreatedById);
        User? acceptor = users.FirstOrDefault(u => u.Id == command.ActorId);
        if (creator is null || acceptor is null)
        {
            return Result.Failure<SupportLinkDto>(UserErrors.NotFound);
        }

        (User learner, User supporter) = invitation.CreatorSide == InvitationSide.Learner
            ? (creator, acceptor)
            : (acceptor, creator);

        IReadOnlyList<SupportLink> learnerLinks = await _links.GetLearnerLinksTrackedAsync(learner.Id, ct);
        Result<SupportLink> created = SupportLinkPolicy.CreateOnAccept(
            Guid.NewGuid(),
            new LinkParty(learner.Id, learner.AgeGroup),
            new LinkParty(supporter.Id, supporter.AgeGroup),
            invitation.Relationship,
            learnerLinks,
            command.ActorId,
            now);
        if (created.IsFailure)
        {
            _logger.LogWarning(
                "AcceptInvitationCommand rejected by policy: InvitationId={InvitationId}, ErrorCode={ErrorCode}",
                invitation.Id, created.Error.Code);
            return Result.Failure<SupportLinkDto>(created.Error);
        }

        SupportLink link = created.Value;
        invitation.Accept(command.ActorId, link.Id, now);
        await _links.AddLinkAsync(link, ct);

        SupportLinkAuditAction action = link.IsActive
            ? SupportLinkAuditAction.LinkActivated
            : SupportLinkAuditAction.LinkPendingPrimaryApproval;
        await _links.AddAuditEntryAsync(new SupportLinkAuditEntry(Guid.NewGuid(), link.Id, command.ActorId, action, now), ct);

        if (link.IsActive)
        {
            await _events.PublishActivatedAsync(link, now, ct);
        }

        Result save = await _links.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("AcceptInvitationCommand failed to persist: {ErrorCode}", save.Error.Code);
            return Result.Failure<SupportLinkDto>(save.Error);
        }

        Guid? primaryId = learnerLinks.FirstOrDefault(l => l.IsPrimary && l.IsActive)?.SupporterId;
        await SupportLinkCache.InvalidateAsync(_cache, ct, learner.Id, supporter.Id, primaryId);

        _logger.LogInformation(
            "AcceptInvitationCommand succeeded: LinkId={LinkId}, Status={Status}, IsPrimary={IsPrimary}",
            link.Id, link.Status, link.IsPrimary);

        Dictionary<Guid, User> byId = new() { [learner.Id] = learner, [supporter.Id] = supporter };
        return Result.Success(SupportLinkDtoMapper.ToDto(link, byId, null, command.ActorId, _options.UnlinkOverrideWaitDays));
    }
}
