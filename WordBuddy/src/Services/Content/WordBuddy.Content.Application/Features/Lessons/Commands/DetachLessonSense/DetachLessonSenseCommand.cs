using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.DetachLessonSense;

/// <summary>Removes a word from a lesson. The sense itself is kept.</summary>
public sealed record DetachLessonSenseCommand(Guid LessonId, Guid SenseId) : ICommand;
