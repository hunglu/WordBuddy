using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.Caching;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.DeleteGroup;

/// <summary>Soft-deletes a group, removes every pending and active member (GroupDeleted) and publishes <c>LearnerGroupDeleted</c>.</summary>
public sealed class DeleteGroupCommandHandler : ICommandHandler<DeleteGroupCommand>
{
    private readonly ILearnerGroupRepository _groups;
    private readonly ILearnerGroupEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<DeleteGroupCommand> _validator;
    private readonly ILogger<DeleteGroupCommandHandler> _logger;

    public DeleteGroupCommandHandler(
        ILearnerGroupRepository groups,
        ILearnerGroupEventPublisher events,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<DeleteGroupCommand> validator,
        ILogger<DeleteGroupCommandHandler> logger)
    {
        _groups = groups;
        _events = events;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(DeleteGroupCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("DeleteGroupCommand started: GroupId={GroupId}, CallerId={CallerId}", command.GroupId, command.CallerId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("DeleteGroupCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("DeleteGroup.Validation", validation.ToString()));
        }

        Result<LearnerGroup> found = await _groups.GetGroupTrackedAsync(command.GroupId, ct);
        if (found.IsFailure)
        {
            return found;
        }

        LearnerGroup group = found.Value;
        if (group.OwnerId != command.CallerId)
        {
            _logger.LogWarning("DeleteGroupCommand rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", group.Id, LearnerGroupErrors.NotOwner.Code);
            return Result.Failure(LearnerGroupErrors.NotOwner);
        }

        DateTime now = _time.GetUtcNow().UtcDateTime;
        Result deleted = group.Delete(now);
        if (deleted.IsFailure)
        {
            return deleted;
        }

        IReadOnlyList<LearnerGroupMember> members = await _groups.GetOpenMembersTrackedAsync(group.Id, ct);
        foreach (LearnerGroupMember member in members)
        {
            member.Remove(GroupMemberRemovedReason.GroupDeleted, now);
        }

        await _events.PublishGroupDeletedAsync(group.Id, group.OwnerId, now, ct);

        Result save = await _groups.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("DeleteGroupCommand failed to persist: {ErrorCode}", save.Error.Code);
            return save;
        }

        await LearnerGroupCache.InvalidateAsync(_cache, [group.Id], ct);
        _logger.LogInformation("DeleteGroupCommand succeeded: GroupId={GroupId}, MemberCount={MemberCount}", group.Id, members.Count);
        return Result.Success();
    }
}
