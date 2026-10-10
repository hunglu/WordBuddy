namespace WordBuddy.Identity.Api.Models;

/// <summary>Body of <c>POST /api/auth/groups</c> and <c>PUT /api/auth/groups/{id}</c>.</summary>
/// <param name="Name">3 to 60 characters.</param>
public sealed record GroupNameRequest(string Name);

/// <summary>Body of <c>POST /api/auth/groups/{id}/members</c>.</summary>
/// <param name="LearnerIds">Learners the caller actively supports.</param>
public sealed record AddGroupMembersRequest(IReadOnlyList<Guid> LearnerIds);
