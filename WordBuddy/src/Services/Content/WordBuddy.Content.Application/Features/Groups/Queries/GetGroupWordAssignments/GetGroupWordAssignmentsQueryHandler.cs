using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Groups.Queries.GetGroupWordAssignments;

/// <summary>Owner only. A group Content has never seen a member of has no history: an empty list, not an error.</summary>
public sealed class GetGroupWordAssignmentsQueryHandler : IQueryHandler<GetGroupWordAssignmentsQuery, IReadOnlyList<GroupWordAssignmentDto>>
{
    private readonly ILearnerGroupProjectionRepository _projection;
    private readonly IGroupWordRepository _words;
    private readonly ILogger<GetGroupWordAssignmentsQueryHandler> _logger;

    public GetGroupWordAssignmentsQueryHandler(
        ILearnerGroupProjectionRepository projection,
        IGroupWordRepository words,
        ILogger<GetGroupWordAssignmentsQueryHandler> logger)
    {
        _projection = projection;
        _words = words;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<GroupWordAssignmentDto>>> HandleAsync(GetGroupWordAssignmentsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetGroupWordAssignmentsQuery started: GroupId={GroupId}, CallerId={CallerId}", query.GroupId, query.CallerId);

        IReadOnlyList<LearnerGroupMemberProjection> rows = await _projection.GetGroupAsync(query.GroupId, ct);
        if (rows.Count == 0)
        {
            return Result.Success<IReadOnlyList<GroupWordAssignmentDto>>([]);
        }

        if (rows[0].OwnerId != query.CallerId)
        {
            _logger.LogWarning("GetGroupWordAssignmentsQuery rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", query.GroupId, GroupWordErrors.Forbidden.Code);
            return Result.Failure<IReadOnlyList<GroupWordAssignmentDto>>(GroupWordErrors.Forbidden);
        }

        IReadOnlyList<GroupWordAssignment> assignments = await _words.GetAssignmentsAsync(query.GroupId, ct);
        IReadOnlyList<Sense> senses = await _words.GetSensesAsync(assignments.Select(a => a.SenseId).Distinct().ToList(), ct);
        Dictionary<Guid, Sense> sensesById = senses.ToDictionary(s => s.Id);

        List<GroupWordAssignmentDto> result = assignments
            .Where(a => sensesById.ContainsKey(a.SenseId))
            .Select(a => new GroupWordAssignmentDto(a.Id, a.SenseId, sensesById[a.SenseId].Word, sensesById[a.SenseId].Definition, a.AssignedAtUtc))
            .ToList();

        _logger.LogInformation("GetGroupWordAssignmentsQuery succeeded: GroupId={GroupId}, Count={Count}", query.GroupId, result.Count);
        return Result.Success<IReadOnlyList<GroupWordAssignmentDto>>(result);
    }
}
