using WordBuddy.Content.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.Lessons.Queries.SearchLessonSenses;

/// <summary>Finds words an admin may attach to a lesson (system words and shared-pool words).
/// <paramref name="Search"/> matches the start or any part of the word; empty returns the first page.</summary>
public sealed record SearchLessonSensesQuery(string? Search) : IQuery<IReadOnlyList<AdminSenseSearchResultDto>>;
