using FluentValidation;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Commands.SubmitQuizAnswer;

public sealed class SubmitQuizAnswerCommandValidator : AbstractValidator<SubmitQuizAnswerCommand>
{
    public SubmitQuizAnswerCommandValidator()
    {
        RuleFor(c => c.QuizId).NotEmpty();
        RuleFor(c => c.QuestionId).NotEmpty();
        RuleFor(c => c.SelectedOptionIndex).GreaterThanOrEqualTo(0);
    }
}
