namespace WordBuddy.Application.Features.Progress.Queries.GetUserProgress;

/// <summary>Query to retrieve all lesson progress records for a specific user.</summary>
public sealed record GetUserProgressQuery(Guid UserId);
