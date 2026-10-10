namespace WordBuddy.Content.Api.Models;

/// <summary>Request body of <c>POST /api/vocabulary/groups/{groupId}/words</c>.</summary>
/// <param name="SenseIds">Senses to give to every active member, 1 to 50.</param>
public sealed record AssignWordsToGroupRequest(IReadOnlyList<Guid> SenseIds);
