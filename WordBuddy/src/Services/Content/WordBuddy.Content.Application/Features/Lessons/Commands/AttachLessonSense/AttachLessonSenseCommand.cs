using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.AttachLessonSense;

/// <summary>Adds a word to the end of a vocabulary lesson. Only system words and shared-pool words qualify.</summary>
public sealed record AttachLessonSenseCommand(Guid LessonId, Guid SenseId) : ICommand;
