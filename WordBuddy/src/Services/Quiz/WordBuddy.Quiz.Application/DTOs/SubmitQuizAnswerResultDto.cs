namespace WordBuddy.Quiz.Application.DTOs;

public sealed record SubmitQuizAnswerResultDto(bool IsCorrect, int CorrectOptionIndex, string Explanation);
