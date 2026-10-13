using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.UnpublishLesson;

/// <summary>Hides a lesson from learners. Safe to repeat.</summary>
public sealed record UnpublishLessonCommand(Guid LessonId) : ICommand;
