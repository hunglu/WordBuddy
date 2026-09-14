using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Application.DTOs;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Commands.SubmitQuizAnswer;

public sealed record SubmitQuizAnswerCommand(Guid QuizId, Guid QuestionId, int SelectedOptionIndex) : ICommand<SubmitQuizAnswerResultDto>;
