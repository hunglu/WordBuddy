using WordBuddy.Domain.Enums;

namespace WordBuddy.Application.Features.Lessons.Commands.CreateLesson;

/// <summary>Command to create a new lesson. Admin-only operation.</summary>
/// <param name="Title">Lesson title shown to learners.</param>
/// <param name="Description">Short description of the lesson content.</param>
/// <param name="Type">Content category.</param>
/// <param name="Level">Target proficiency level.</param>
/// <param name="TargetAgeGroup">Intended audience age group.</param>
/// <param name="OrderIndex">Display position within the lesson catalogue.</param>
public sealed record CreateLessonCommand(
    string Title,
    string Description,
    LessonType Type,
    Level Level,
    TargetAgeGroup TargetAgeGroup,
    int OrderIndex);
