using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Api.Models;

/// <summary>Request body to change the details of a lesson. The lesson type cannot change.</summary>
public sealed record UpdateLessonRequest(string Title, string Description, Level Level, AgeGroup TargetAgeGroup);

/// <summary>Request body to add a word to a lesson.</summary>
public sealed record AttachLessonSenseRequest(Guid SenseId);

/// <summary>Request body to set the word order of a lesson: every attached sense id, once, in the new order.</summary>
public sealed record ReorderLessonSensesRequest(IReadOnlyList<Guid> SenseIds);
