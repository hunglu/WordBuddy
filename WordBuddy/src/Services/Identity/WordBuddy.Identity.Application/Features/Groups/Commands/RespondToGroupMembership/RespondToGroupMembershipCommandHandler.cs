using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.RespondToGroupMembership;

/// <summary>Only the active Primary of the child may approve (publishes <c>LearnerGroupMemberActivated</c>) or reject.</summary>
public sealed class RespondToGroupMembershipCommandHandler : ICommandHandler<RespondToGroupMembershipCommand>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly ILearnerGroupRepository _groups;
    private readonly ILearnerGroupEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<RespondToGroupMembershipCommand> _validator;
    private readonly ILogger<RespondToGroupMembershipCommandHandler> _logger;

    public RespondToGroupMembershipCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        ILearnerGroupRepository groups,
        ILearnerGroupEventPublisher events,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<RespondToGroupMembershipCommand> validator,
        ILogger<RespondToGroupMembershipCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _groups = groups;
        _events = events;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RespondToGroupMembershipCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RespondToGroupMembershipCommand started: MemberId={MemberId}, CallerId={CallerId}, Approve={Approve}",
            command.MemberId, command.CallerId, command.Approve);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RespondToGroupMembershipCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RespondToGroupMembership.Validation", validation.ToString()));
        }

        Result<LearnerGroupMember> memberResult = await _groups.GetMemberTrackedAsync(command.MemberId, ct);
        if (memberResult.IsFailure)
        {
            return memberResult;
        }

        LearnerGroupMember member = memberResult.Value;
        Result<LearnerGroup> groupResult = await _groups.GetGroupTrackedAsync(member.GroupId, ct);
        if (groupResult.IsFailure)
        {
            return groupResult;
        }

        Result<User> caller = await _users.GetByIdAsync(command.CallerId, ct);
        if (caller.IsFailure)
        {
            return caller;
        }

        Guid? primaryId = await _links.GetActivePrimarySupporterIdAsync(member.LearnerId, ct);
        Result allowed = LearnerGroupPolicy.CanRespond(member, new LinkParty(caller.Value.Id, caller.Value.AgeGroup), primaryId);
        if (allowed.IsFailure)
        {
            _logger.LogWarning(
                "RespondToGroupMembershipCommand rejected: MemberId={MemberId}, ErrorCode={ErrorCode}", member.Id, allowed.Error.Code);
            return allowed;
        }

        DateTime now = _time.GetUtcNow().UtcDateTime;
        Result change = command.Approve ? member.Approve(now) : member.Reject(now);
        if (change.IsFailure)
        {
            return change;
        }

        if (command.Approve)
        {
            await _events.PublishMemberActivatedAsync(member.GroupId, groupResult.Value.OwnerId, member.LearnerId, now, ct);
        }

        Result save = await _groups.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("RespondToGroupMembershipCommand failed to persist: {ErrorCode}", save.Error.Code);
            return save;
        }

        await LearnerGroupCache.InvalidateAsync(_cache, [member.GroupId], ct);
        _logger.LogInformation(
            "RespondToGroupMembershipCommand succeeded: MemberId={MemberId}, Status={Status}", member.Id, member.Status);
        return Result.Success();
    }
}
