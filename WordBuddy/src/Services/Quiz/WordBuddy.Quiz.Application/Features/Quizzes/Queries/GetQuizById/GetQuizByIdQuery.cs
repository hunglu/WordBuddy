using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Application.DTOs;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Queries.GetQuizById;

public sealed record GetQuizByIdQuery(Guid QuizId) : IQuery<QuizDto>;
