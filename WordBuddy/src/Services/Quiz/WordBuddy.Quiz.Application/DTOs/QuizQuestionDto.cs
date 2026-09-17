using WordBuddy.Quiz.Domain;

namespace WordBuddy.Quiz.Application.DTOs;

/// <summary>A question as presented to a learner — deliberately omits <c>CorrectOptionIndex</c>
/// and <c>Explanation</c> so the answer isn't visible before submitting.</summary>
public sealed record QuizQuestionDto(Guid Id, string Text, QuizQuestionType Type, IReadOnlyList<string> Options);
