using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

public sealed class GrammarRule : Entity
{
    public Guid LessonId { get; }
    public string Title { get; }
    public string Explanation { get; }
    public IReadOnlyList<string> Examples { get; }

    public GrammarRule(Guid id, Guid lessonId, string title, string explanation, IReadOnlyList<string> examples)
        : base(id)
    {
        LessonId = lessonId;
        Title = title;
        Explanation = explanation;
        Examples = examples;
    }
}
