namespace WordBuddy.Domain.Entities;

/// <summary>A grammar rule with explanation and ordered usage examples.</summary>
public sealed class GrammarRule
{
    /// <summary>Initializes a new <see cref="GrammarRule"/>.</summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="lessonId">Identifier of the parent lesson.</param>
    /// <param name="ruleName">Short name of the rule (e.g. "Present Perfect").</param>
    /// <param name="explanation">Detailed explanation of the grammar rule.</param>
    /// <param name="orderIndex">Display order within the lesson.</param>
    /// <param name="examples">Example sentences illustrating the rule.</param>
    public GrammarRule(
        Guid id,
        Guid lessonId,
        string ruleName,
        string explanation,
        int orderIndex,
        IReadOnlyList<string> examples)
    {
        Id = id;
        LessonId = lessonId;
        RuleName = ruleName;
        Explanation = explanation;
        OrderIndex = orderIndex;
        Examples = examples;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the identifier of the parent lesson.</summary>
    public Guid LessonId { get; }

    /// <summary>Gets the short name of the rule (e.g. "Present Perfect").</summary>
    public string RuleName { get; }

    /// <summary>Gets the detailed explanation of the grammar rule.</summary>
    public string Explanation { get; }

    /// <summary>Gets the display order within the lesson.</summary>
    public int OrderIndex { get; }

    /// <summary>Gets the example sentences illustrating the rule.</summary>
    public IReadOnlyList<string> Examples { get; }
}
