using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.RemoveGroupMember;

/// <summary>Owner removes a member (ByOwner). Publishes <c>LearnerGroupMemberRemoved</c> when the member was active.</summary>
public sealed class RemoveGroupMemberCommandHandler : ICommandHandler<RemoveGroupMemberCommand>
{
    private readonly ILearnerGroupRepository _groups;
    private readonly ILearnerGroupEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<RemoveGroupMemberCommand> _validator;
    private readonly ILogger<RemoveGroupMemberCommandHandler> _logger;

    public RemoveGroupMemberCommandHandler(
        ILearnerGroupRepository groups,
        ILearnerGroupEventPublisher events,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<RemoveGroupMemberCommand> validator,
        ILogger<RemoveGroupMemberCommandHandler> logger)
    {
        _groups = groups;
        _events = events;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(RemoveGroupMemberCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "RemoveGroupMemberCommand started: GroupId={GroupId}, CallerId={CallerId}, LearnerId={LearnerId}",
            command.GroupId, command.CallerId, command.LearnerId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RemoveGroupMemberCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("RemoveGroupMember.Validation", validation.ToString()));
        }

        Result<LearnerGroup> found = await _groups.GetGroupTrackedAsync(command.GroupId, ct);
        if (found.IsFailure)
        {
            return found;
        }

        LearnerGroup group = found.Value;
        if (group.OwnerId != command.CallerId)
        {
            _logger.LogWarning("RemoveGroupMemberCommand rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", group.Id, LearnerGroupErrors.NotOwner.Code);
            return Result.Failure(LearnerGroupErrors.NotOwner);
        }

        Result<LearnerGroupMember> memberResult = await _groups.GetOpenMemberTrackedAsync(group.Id, command.LearnerId, ct);
        if (memberResult.IsFailure)
        {
            return memberResult;
        }

        LearnerGroupMember member = memberResult.Value;
        bool wasActive = member.IsActive;
        DateTime now = _time.GetUtcNow().UtcDateTime;
        Result removed = member.Remove(GroupMemberRemovedReason.ByOwner, now);
        if (removed.IsFailure)
        {
            return removed;
        }

        if (wasActive)
        {
            await _events.PublishMemberRemovedAsync(group.Id, group.OwnerId, member.LearnerId, GroupMemberRemovedReason.ByOwner, now, ct);
        }

        Result save = await _groups.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("RemoveGroupMemberCommand failed to persist: {ErrorCode}", save.Error.Code);
            return save;
        }

        await LearnerGroupCache.InvalidateAsync(_cache, [group.Id], ct);
        _logger.LogInformation("RemoveGroupMemberCommand succeeded: GroupId={GroupId}, LearnerId={LearnerId}", group.Id, member.LearnerId);
        return Result.Success();
    }
}
