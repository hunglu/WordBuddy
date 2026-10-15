using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetGroup;

/// <summary>A group with member counts and the ids of its learners. Owner only.</summary>
public sealed record GetGroupQuery(Guid GroupId, Guid CallerId) : IQuery<LearnerGroupDetailDto>;
