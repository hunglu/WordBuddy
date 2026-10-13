using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Commands.ReorderLessonSenses;

/// <summary>Sets the word order of a lesson. <paramref name="SenseIds"/> must list every attached word once.</summary>
public sealed record ReorderLessonSensesCommand(Guid LessonId, IReadOnlyList<Guid> SenseIds) : ICommand;
