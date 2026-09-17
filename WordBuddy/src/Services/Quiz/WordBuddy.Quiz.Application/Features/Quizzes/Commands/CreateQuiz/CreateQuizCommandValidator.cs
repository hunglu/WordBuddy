using FluentValidation;

namespace WordBuddy.Quiz.Application.Features.Quizzes.Commands.CreateQuiz;

public sealed class CreateQuizCommandValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizCommandValidator()
    {
        RuleFor(c => c.LessonId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).NotEmpty().MaximumLength(2000);
        RuleFor(c => c.Level).IsInEnum();
        RuleFor(c => c.TargetAgeGroup).IsInEnum();
        RuleFor(c => c.Questions).NotEmpty();

        RuleForEach(c => c.Questions).ChildRules(question =>
        {
            question.RuleFor(q => q.Text).NotEmpty().MaximumLength(500);
            question.RuleFor(q => q.Type).IsInEnum();
            question.RuleFor(q => q.Options).Must(o => o.Count >= 2).WithMessage("A question needs at least 2 options.");
            question.RuleFor(q => q.Explanation).NotEmpty().MaximumLength(1000);
            question.RuleFor(q => q).Must(q => q.CorrectOptionIndex >= 0 && q.CorrectOptionIndex < q.Options.Count)
                .WithMessage("CorrectOptionIndex must be a valid index into Options.");
        });
    }
}
