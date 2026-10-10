using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;

namespace WordBuddy.Content.Application.Features.Groups.Queries.GetGroupWordAssignments;

/// <summary>The assignment history of a group, newest first. Owner only.</summary>
public sealed record GetGroupWordAssignmentsQuery(Guid GroupId, Guid CallerId) : IQuery<IReadOnlyList<GroupWordAssignmentDto>>;
