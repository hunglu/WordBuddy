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

namespace WordBuddy.Identity.Application.Features.Groups.Commands.LeaveGroup;

/// <summary>An adult member leaves (Left). A child gets 403. Publishes <c>LearnerGroupMemberRemoved</c> when the member was active.</summary>
public sealed class LeaveGroupCommandHandler : ICommandHandler<LeaveGroupCommand>
{
    private readonly IUserRepository _users;
    private readonly ILearnerGroupRepository _groups;
    private readonly ILearnerGroupEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly TimeProvider _time;
    private readonly IValidator<LeaveGroupCommand> _validator;
    private readonly ILogger<LeaveGroupCommandHandler> _logger;

    public LeaveGroupCommandHandler(
        IUserRepository users,
        ILearnerGroupRepository groups,
        ILearnerGroupEventPublisher events,
        IDistributedCache cache,
        TimeProvider time,
        IValidator<LeaveGroupCommand> validator,
        ILogger<LeaveGroupCommandHandler> logger)
    {
        _users = users;
        _groups = groups;
        _events = events;
        _cache = cache;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result> HandleAsync(LeaveGroupCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("LeaveGroupCommand started: GroupId={GroupId}, LearnerId={LearnerId}", command.GroupId, command.LearnerId);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("LeaveGroupCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure(Error.Validation("LeaveGroup.Validation", validation.ToString()));
        }

        Result<User> user = await _users.GetByIdAsync(command.LearnerId, ct);
        if (user.IsFailure)
        {
            return user;
        }

        Result<LearnerGroup> groupResult = await _groups.GetGroupTrackedAsync(command.GroupId, ct);
        if (groupResult.IsFailure)
        {
            return groupResult;
        }

        Result<LearnerGroupMember> memberResult = await _groups.GetOpenMemberTrackedAsync(command.GroupId, command.LearnerId, ct);
        if (memberResult.IsFailure)
        {
            return memberResult;
        }

        LearnerGroupMember member = memberResult.Value;
        Result allowed = LearnerGroupPolicy.CanLeave(member, new LinkParty(user.Value.Id, user.Value.AgeGroup));
        if (allowed.IsFailure)
        {
            _logger.LogWarning("LeaveGroupCommand rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", command.GroupId, allowed.Error.Code);
            return allowed;
        }

        bool wasActive = member.IsActive;
        DateTime now = _time.GetUtcNow().UtcDateTime;
        Result removed = member.Remove(GroupMemberRemovedReason.Left, now);
        if (removed.IsFailure)
        {
            return removed;
        }

        if (wasActive)
        {
            await _events.PublishMemberRemovedAsync(
                command.GroupId, groupResult.Value.OwnerId, member.LearnerId, GroupMemberRemovedReason.Left, now, ct);
        }

        Result save = await _groups.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("LeaveGroupCommand failed to persist: {ErrorCode}", save.Error.Code);
            return save;
        }

        await LearnerGroupCache.InvalidateAsync(_cache, [command.GroupId], ct);
        _logger.LogInformation("LeaveGroupCommand succeeded: GroupId={GroupId}, LearnerId={LearnerId}", command.GroupId, command.LearnerId);
        return Result.Success();
    }
}
