using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.UpdateLesson;

/// <summary>Changes a lesson's details. The lesson type cannot change.</summary>
public sealed record UpdateLessonCommand(Guid LessonId, string Title, string Description, Level Level, AgeGroup TargetAgeGroup) : ICommand;
