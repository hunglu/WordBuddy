using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.PublishLesson;

/// <summary>Makes a lesson visible to learners. Safe to repeat.</summary>
public sealed record PublishLessonCommand(Guid LessonId) : ICommand;
