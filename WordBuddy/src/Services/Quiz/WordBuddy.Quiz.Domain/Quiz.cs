using WordBuddy.Shared.Kernel;

namespace WordBuddy.Quiz.Domain;

/// <summary>A set of questions testing a lesson's content. Aggregate root for its <see cref="QuizQuestion"/> children.</summary>
public sealed class Quiz : Entity
{
    private readonly List<QuizQuestion> _questions = [];

    /// <summary>The Content service's Lesson this quiz tests — a plain <see cref="Guid"/>, not a
    /// foreign key, since services never reference each other's data directly.</summary>
    public Guid LessonId { get; }

    public string Title { get; }
    public string Description { get; }
    public Level Level { get; }
    public AgeGroup TargetAgeGroup { get; }

    public IReadOnlyList<QuizQuestion> Questions => _questions;

    public Quiz(Guid id, Guid lessonId, string title, string description, Level level, AgeGroup targetAgeGroup)
        : base(id)
    {
        LessonId = lessonId;
        Title = title;
        Description = description;
        Level = level;
        TargetAgeGroup = targetAgeGroup;
    }

    public void AddQuestion(QuizQuestion question) => _questions.Add(question);
}
