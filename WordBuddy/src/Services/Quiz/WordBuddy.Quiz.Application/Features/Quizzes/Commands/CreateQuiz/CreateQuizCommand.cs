using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Domain;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Commands.CreateQuiz;

public sealed record CreateQuizQuestionInput(string Text, QuizQuestionType Type, IReadOnlyList<string> Options, int CorrectOptionIndex, string Explanation);

public sealed record CreateQuizCommand(
    Guid LessonId,
    string Title,
    string Description,
    Level Level,
    AgeGroup TargetAgeGroup,
    IReadOnlyList<CreateQuizQuestionInput> Questions) : ICommand<Guid>;
