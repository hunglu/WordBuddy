namespace WordBuddy.API.Models.Requests;

/// <summary>Request body for recording or updating a lesson progress entry.</summary>
public sealed record RecordProgressRequest(
    Guid LessonId,
    bool IsCompleted,
    int? ScorePercent);
