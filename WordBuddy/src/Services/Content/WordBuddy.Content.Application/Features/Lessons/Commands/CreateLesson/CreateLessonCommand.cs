using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.CreateLesson;

public sealed record CreateLessonCommand(
    string Title,
    string Description,
    LessonType Type,
    Level Level,
    AgeGroup TargetAgeGroup) : ICommand<Guid>;
