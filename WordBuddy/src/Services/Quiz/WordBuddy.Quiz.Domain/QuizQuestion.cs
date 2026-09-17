using WordBuddy.Shared.Kernel;

namespace WordBuddy.Quiz.Domain;

public sealed class QuizQuestion : Entity
{
    public Guid QuizId { get; }
    public string Text { get; }
    public QuizQuestionType Type { get; }
    public IReadOnlyList<string> Options { get; }
    public int CorrectOptionIndex { get; }
    public string Explanation { get; }

    public QuizQuestion(
        Guid id,
        Guid quizId,
        string text,
        QuizQuestionType type,
        IReadOnlyList<string> options,
        int correctOptionIndex,
        string explanation)
        : base(id)
    {
        QuizId = quizId;
        Text = text;
        Type = type;
        Options = options;
        CorrectOptionIndex = correctOptionIndex;
        Explanation = explanation;
    }

    /// <summary>Evaluates whether <paramref name="selectedOptionIndex"/> is the correct answer.</summary>
    public bool IsCorrect(int selectedOptionIndex) => selectedOptionIndex == CorrectOptionIndex;
}
