using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.DeleteLesson;

/// <summary>Deletes a lesson and its word links. The senses themselves are kept.</summary>
public sealed record DeleteLessonCommand(Guid LessonId) : ICommand;
