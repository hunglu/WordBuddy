using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.Groups.Queries.GetGroupWordAssignments;

/// <summary>Owner only. A group Content has never seen a member of has no history: an empty page, not an error.</summary>
public sealed class GetGroupWordAssignmentsQueryHandler : IQueryHandler<GetGroupWordAssignmentsQuery, PagedResult<GroupWordAssignmentDto>>
{
    private readonly ILearnerGroupProjectionRepository _projection;
    private readonly IGroupWordRepository _words;
    private readonly IValidator<GetGroupWordAssignmentsQuery> _validator;
    private readonly ILogger<GetGroupWordAssignmentsQueryHandler> _logger;

    public GetGroupWordAssignmentsQueryHandler(
        ILearnerGroupProjectionRepository projection,
        IGroupWordRepository words,
        IValidator<GetGroupWordAssignmentsQuery> validator,
        ILogger<GetGroupWordAssignmentsQueryHandler> logger)
    {
        _projection = projection;
        _words = words;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Result<PagedResult<GroupWordAssignmentDto>>> HandleAsync(GetGroupWordAssignmentsQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "GetGroupWordAssignmentsQuery started: GroupId={GroupId}, CallerId={CallerId}, Page={Page}, PageSize={PageSize}",
            query.GroupId, query.CallerId, query.Page, query.PageSize);

        ValidationResult validation = await _validator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("GetGroupWordAssignmentsQuery validation failed: {Errors}", validation.ToString());
            return Result.Failure<PagedResult<GroupWordAssignmentDto>>(Error.Validation("GetGroupWordAssignments.Validation", validation.ToString()));
        }

        IReadOnlyList<LearnerGroupMemberProjection> rows = await _projection.GetGroupAsync(query.GroupId, ct);
        if (rows.Count == 0)
        {
            return Result.Success(new PagedResult<GroupWordAssignmentDto>([], query.Page, query.PageSize, 0));
        }

        if (rows[0].OwnerId != query.CallerId)
        {
            _logger.LogWarning("GetGroupWordAssignmentsQuery rejected: GroupId={GroupId}, ErrorCode={ErrorCode}", query.GroupId, GroupWordErrors.Forbidden.Code);
            return Result.Failure<PagedResult<GroupWordAssignmentDto>>(GroupWordErrors.Forbidden);
        }

        PagedResult<GroupWordAssignment> assignments = await _words.GetAssignmentsAsync(query.GroupId, query.Page, query.PageSize, ct);
        IReadOnlyList<Sense> senses = await _words.GetSensesAsync(assignments.Items.Select(a => a.SenseId).Distinct().ToList(), ct);
        Dictionary<Guid, Sense> sensesById = senses.ToDictionary(s => s.Id);

        List<GroupWordAssignmentDto> items = assignments.Items
            .Where(a => sensesById.ContainsKey(a.SenseId))
            .Select(a => new GroupWordAssignmentDto(a.Id, a.SenseId, sensesById[a.SenseId].Word, sensesById[a.SenseId].Definition, a.AssignedAtUtc))
            .ToList();

        _logger.LogInformation("GetGroupWordAssignmentsQuery succeeded: GroupId={GroupId}, Count={Count}, TotalCount={TotalCount}", query.GroupId, items.Count, assignments.TotalCount);
        return Result.Success(new PagedResult<GroupWordAssignmentDto>(items, assignments.Page, assignments.PageSize, assignments.TotalCount));
    }
}
