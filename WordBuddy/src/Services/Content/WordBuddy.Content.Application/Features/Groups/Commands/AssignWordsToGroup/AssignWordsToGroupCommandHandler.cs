using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Groups.Commands.AssignWordsToGroup;

/// <summary>
/// Gives each active member of the caller's group the chosen senses. Content does not know a member's
/// age group (events carry ids only), so every member is treated as a possible child:
/// <list type="bullet">
/// <item>A sense cleared for children is linked as is.</item>
/// <item>An auto-filled sense waiting for the global admin approval is linked and approved for that
/// member by the assigning supporter (same rule as the single-child supporter approval).</item>
/// <item>Any other sense (for example adult-only) is skipped and counted in <c>SkippedForChildren</c>.</item>
/// </list>
/// A member whose support link is not active in the local projection is left out (defence in depth).
/// </summary>
public sealed class AssignWordsToGroupCommandHandler : ICommandHandler<AssignWordsToGroupCommand, GroupWordAssignmentResultDto>
{
    private readonly ILearnerGroupProjectionRepository _projection;
    private readonly ISupportLinkProjectionRepository _supportLinks;
    private readonly IGroupWordRepository _words;
    private readonly TimeProvider _time;
    private readonly IValidator<AssignWordsToGroupCommand> _validator;
    private readonly ILogger<AssignWordsToGroupCommandHandler> _logger;

    public AssignWordsToGroupCommandHandler(
        ILearnerGroupProjectionRepository projection,
        ISupportLinkProjectionRepository supportLinks,
        IGroupWordRepository words,
        TimeProvider time,
        IValidator<AssignWordsToGroupCommand> validator,
        ILogger<AssignWordsToGroupCommandHandler> logger)
    {
        _projection = projection;
        _supportLinks = supportLinks;
        _words = words;
        _time = time;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<GroupWordAssignmentResultDto>> HandleAsync(AssignWordsToGroupCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AssignWordsToGroupCommand started: GroupId={GroupId}, CallerId={CallerId}, SenseCount={SenseCount}",
            command.GroupId, command.CallerId, command.SenseIds?.Count ?? 0);

        ValidationResult validation = await _validator.ValidateAsync(command, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("AssignWordsToGroupCommand validation failed: {Errors}", validation.ToString());
            return Result.Failure<GroupWordAssignmentResultDto>(Error.Validation("AssignWordsToGroup.Validation", validation.ToString()));
        }

        IReadOnlyList<LearnerGroupMemberProjection> rows = await _projection.GetGroupAsync(command.GroupId, ct);
        if (rows.Count == 0)
        {
            return Result.Failure<GroupWordAssignmentResultDto>(GroupWordErrors.NoActiveMembers);
        }

        if (rows[0].OwnerId != command.CallerId)
        {
            _logger.LogWarning("AssignWordsToGroupCommand rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", command.GroupId, GroupWordErrors.Forbidden.Code);
            return Result.Failure<GroupWordAssignmentResultDto>(GroupWordErrors.Forbidden);
        }

        List<Guid> activeMemberIds = rows.Where(r => r.IsActive).Select(r => r.LearnerId).ToList();
        IReadOnlySet<Guid> supported = await _supportLinks.GetLearnersWithActiveLinkAsync(command.CallerId, activeMemberIds, ct);
        List<Guid> memberIds = activeMemberIds.Where(supported.Contains).ToList();
        if (memberIds.Count == 0)
        {
            return Result.Failure<GroupWordAssignmentResultDto>(GroupWordErrors.NoActiveMembers);
        }

        List<Guid> senseIds = command.SenseIds.Distinct().ToList();
        IReadOnlyList<Sense> senses = await _words.GetSensesAsync(senseIds, ct);
        if (senses.Count != senseIds.Count || senses.Any(s => !IsAssignable(s)))
        {
            _logger.LogWarning("AssignWordsToGroupCommand rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", command.GroupId, GroupWordErrors.SenseNotFound.Code);
            return Result.Failure<GroupWordAssignmentResultDto>(GroupWordErrors.SenseNotFound);
        }

        IReadOnlySet<(Guid UserId, Guid SenseId)> existing = await _words.GetExistingLinksAsync(memberIds, senseIds, ct);

        List<LearnerWord> newLinks = [];
        int alreadyHad = 0;
        int skipped = 0;
        foreach (Sense sense in senses)
        {
            bool childCleared = sense.IsVisibleTo(Guid.Empty, AgeGroup.Child);
            bool supporterApprovable = !childCleared && sense.Origin == SenseOrigin.AutoFill;

            foreach (Guid memberId in memberIds)
            {
                if (existing.Contains((memberId, sense.Id)))
                {
                    alreadyHad++;
                    continue;
                }

                if (!childCleared && !supporterApprovable)
                {
                    skipped++;
                    continue;
                }

                LearnerWord link = LearnerWord.CreateBySupporter(memberId, sense.Id, command.CallerId);
                if (supporterApprovable)
                {
                    Result approved = link.ApproveForChild(command.CallerId);
                    if (approved.IsFailure)
                    {
                        return Result.Failure<GroupWordAssignmentResultDto>(approved.Error);
                    }
                }

                newLinks.Add(link);
            }
        }

        DateTime now = _time.GetUtcNow().UtcDateTime;
        await _words.AddLinksAsync(newLinks, ct);
        await _words.AddAssignmentsAsync(
            senseIds.Select(id => new GroupWordAssignment(Guid.NewGuid(), command.GroupId, id, command.CallerId, now)).ToList(),
            ct);

        Result save = await _words.SaveChangesAsync(ct);
        if (save.IsFailure)
        {
            _logger.LogWarning("AssignWordsToGroupCommand failed to persist: {ErrorCode}", save.Error.Code);
            return Result.Failure<GroupWordAssignmentResultDto>(save.Error);
        }

        GroupWordAssignmentResultDto result = new(newLinks.Count, alreadyHad, skipped);
        _logger.LogInformation(
            "AssignWordsToGroupCommand succeeded: GroupId={GroupId}, Added={Added}, AlreadyHad={AlreadyHad}, Skipped={Skipped}",
            command.GroupId, result.Added, result.AlreadyHad, result.SkippedForChildren);
        return Result.Success(result);
    }

    /// <summary>A sense that is not private to its author: shared in the pool, or a system (lesson) word.</summary>
    private static bool IsAssignable(Sense sense) =>
        sense.ShareStatus == VocabularyShareStatus.Shared || sense.Source == VocabularySource.System;
}
