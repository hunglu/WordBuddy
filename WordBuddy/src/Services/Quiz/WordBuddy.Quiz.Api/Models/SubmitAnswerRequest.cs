namespace WordBuddy.Quiz.Api.Models;

/// <summary>Request body for submitting an answer to one quiz question.</summary>
public sealed record SubmitAnswerRequest(Guid QuestionId, int SelectedOptionIndex);
