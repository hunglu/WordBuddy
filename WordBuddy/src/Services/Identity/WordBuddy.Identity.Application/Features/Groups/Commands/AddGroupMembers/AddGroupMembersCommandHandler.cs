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
using WordBuddy.Identity.Domain.Groups;
using WordBuddy.Identity.Domain.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Groups.Commands.AddGroupMembers;

/// <summary>
/// Adds learners one by one through <see cref="LearnerGroupPolicy"/>. A learner that fails a rule is
/// reported as Rejected with the error code; the others are still added. Active members publish
/// <c>LearnerGroupMemberActivated</c>; pending children publish nothing until the Primary approves.
/// </summary>
public sealed class AddGroupMembersCommandHandler : ICommandHandler<AddGroupMembersCommand, IReadOnlyList<AddMemberResultDto>>
{
    private readonly IUserRepository _users;
    private readonly ISupportLinkRepository _links;
    private readonly ILearnerGroupRepository _groups;
    private readonly ILearnerGroupEventPublisher _events;
    private readonly IDistributedCache _cache;
    private readonly LearnerGroupOptions _options;
    private readonly TimeProvider _time;
    private readonly IValidator<AddGroupMembersCommand> _validator;
    private readonly ILogger<AddGroupMembersCommandHandler> _logger;

    public AddGroupMembersCommandHandler(
        IUserRepository users,
        ISupportLinkRepository links,
        ILearnerGroupRepository groups,
        ILearnerGroupEventPublisher events,
        IDistributedCache cache,
        LearnerGroupOptions options,
        TimeProvider time,
        IValidator<AddGroupMembersCommand> validator,
        ILogger<AddGroupMembersCommandHandler> logger)
    {
        _users = users;
        _links = links;
        _groups = groups;
        _events = events;
        _cache = cache;
        _options = options;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AddMemberResultDto>>> HandleAsync(AddGroupMembersCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AddGroupMembersCommand started: GroupId={GroupId}, CallerId={CallerId}, LearnerCount={LearnerCount}",
            command.GroupId, command.CallerId, command.LearnerIds?.Count ?? 0);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AddGroupMembersCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<IReadOnlyList<AddMemberResultDto>>(Error.Validation("AddGroupMembers.Validation", validation.ToString()));
        }

        Result<LearnerGroup> found = await _groups.GetGroupTrackedAsync(command.GroupId, ct);
        if (found.IsFailure)
        {
            return Result.Failure<IReadOnlyList<AddMemberResultDto>>(found.Error);
        }

        LearnerGroup group = found.Value;
        if (group.OwnerId != command.CallerId)
        {
            _logger.LogWarning("AddGroupMembersCommand rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", group.Id, LearnerGroupErrors.NotOwner.Code);
            return Result.Failure<IReadOnlyList<AddMemberResultDto>>(LearnerGroupErrors.NotOwner);
        }

        Result<User> ownerResult = await _users.GetByIdAsync(command.CallerId, ct);
        if (ownerResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<AddMemberResultDto>>(ownerResult.Error);
        }

        LinkParty owner = new(ownerResult.Value.Id, ownerResult.Value.AgeGroup);
        List<Guid> learnerIds = command.LearnerIds.Distinct().ToList();
        IReadOnlyList<User> learners = await _users.GetByIdsAsync(learnerIds, ct);
        Dictionary<Guid, User> learnersById = learners.ToDictionary(u => u.Id);
        IReadOnlyList<SupportLink> ownerLinks = await _links.GetLinksForUserAsync(command.CallerId, ct);
        Dictionary<Guid, SupportLink> activeLinkByLearner = ownerLinks
            .Where(l => l.SupporterId == command.CallerId && l.IsActive)
            .ToDictionary(l => l.LearnerId);

        IReadOnlyList<LearnerGroupMember> openMembers = await _groups.GetOpenMembersTrackedAsync(group.Id, ct);
        HashSet<Guid> memberLearnerIds = openMembers.Select(m => m.LearnerId).ToHashSet();
        int memberCount = openMembers.Count;

        DateTime now = _time.GetUtcNow().UtcDateTime;
        List<AddMemberResultDto> results = [];
        foreach (Guid learnerId in learnerIds)
        {
            if (!learnersById.TryGetValue(learnerId, out User? learner))
            {
                results.Add(new AddMemberResultDto(learnerId, AddMemberOutcome.Rejected, LearnerGroupErrors.LearnerNotFound.Code));
                continue;
            }

            activeLinkByLearner.TryGetValue(learnerId, out SupportLink? link);
            Result<LearnerGroupMember> added = LearnerGroupPolicy.AddMember(
                Guid.NewGuid(),
                group,
                owner,
                new LinkParty(learner.Id, learner.AgeGroup),
                ownerHasActiveLink: link is not null,
                ownerIsLearnerPrimary: link is { IsPrimary: true },
                learnerIsAlreadyMember: memberLearnerIds.Contains(learnerId),
                currentMemberCount: memberCount,
                _options.MaxMembersPerGroup,
                now);
            if (added.IsFailure)
            {
                _logger.LogWarning(
                    "AddGroupMembersCommand learner rejected: GroupId={GroupId}, LearnerId={LearnerId}, ErrorCode={ErrorCode}",
                    group.Id, learnerId, added.Error.Code);
                results.Add(new AddMemberResultDto(learnerId, AddMemberOutcome.Rejected, added.Error.Code));
                continue;
            }

            LearnerGroupMember member = added.Value;
            await _groups.AddMemberAsync(member, ct);
            memberLearnerIds.Add(learnerId);
            memberCount++;

            if (member.IsActive)
            {
                await _events.PublishMemberActivatedAsync(group.Id, group.OwnerId, learnerId, now, ct);
                results.Add(new AddMemberResultDto(learnerId, AddMemberOutcome.Added, null));
            }
            else
            {
                results.Add(new AddMemberResultDto(learnerId, AddMemberOutcome.PendingApproval, null));
            }
        }

        Result save = await _groups.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("AddGroupMembersCommand failed to persist: {ErrorCode}", save.Error.Code);
            return Result.Failure<IReadOnlyList<AddMemberResultDto>>(save.Error);
        }

        await LearnerGroupCache.InvalidateAsync(_cache, [group.Id], ct);
        _logger.LogInformation(
            "AddGroupMembersCommand succeeded: GroupId={GroupId}, Added={Added}, Pending={Pending}, Rejected={Rejected}",
            group.Id,
            results.Count(r => r.Outcome == AddMemberOutcome.Added),
            results.Count(r => r.Outcome == AddMemberOutcome.PendingApproval),
            results.Count(r => r.Outcome == AddMemberOutcome.Rejected));
        return Result.Success<IReadOnlyList<AddMemberResultDto>>(results);
    }
}
