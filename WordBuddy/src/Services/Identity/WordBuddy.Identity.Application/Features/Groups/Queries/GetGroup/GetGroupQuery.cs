using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;

namespace WordBuddy.Identity.Application.Features.Groups.Queries.GetGroup;

/// <summary>A group with its pending and active members. Owner only.</summary>
public sealed record GetGroupQuery(Guid GroupId, Guid CallerId) : IQuery<LearnerGroupDetailDto>;
