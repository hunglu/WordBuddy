using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Application.DTOs;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Queries.GetQuizzes;

public sealed record GetQuizzesQuery(Guid? LessonId) : IQuery<IReadOnlyList<QuizSummaryDto>>;
