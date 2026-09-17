using WordBuddy.Progress.Application.Abstractions;

namespace WordBuddy.Progress.Application.Features.Progress.Commands.RecordProgress;

/// <summary><paramref name="UserId"/> comes from the authenticated caller's JWT — never trust a client-supplied user id.</summary>
public sealed record RecordProgressCommand(Guid UserId, Guid LessonId, bool IsCompleted, int? ScorePercent) : ICommand;
