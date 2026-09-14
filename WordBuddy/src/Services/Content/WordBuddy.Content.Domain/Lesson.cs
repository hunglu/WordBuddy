using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>A structured learning unit (vocabulary, grammar, or daily phrases). Aggregate root
/// for its <see cref="VocabularyItem"/>/<see cref="GrammarRule"/>/<see cref="DailyPhrase"/>
/// children — only the collection matching <see cref="Type"/> is expected to be populated.</summary>
public sealed class Lesson : Entity
{
    private readonly List<VocabularyItem> _vocabularyItems = [];
    private readonly List<GrammarRule> _grammarRules = [];
    private readonly List<DailyPhrase> _dailyPhrases = [];

    public string Title { get; }
    public string Description { get; }
    public LessonType Type { get; }
    public Level Level { get; }
    public AgeGroup TargetAgeGroup { get; }
    public bool IsPublished { get; private set; }

    public IReadOnlyList<VocabularyItem> VocabularyItems => _vocabularyItems;
    public IReadOnlyList<GrammarRule> GrammarRules => _grammarRules;
    public IReadOnlyList<DailyPhrase> DailyPhrases => _dailyPhrases;

    public Lesson(Guid id, string title, string description, LessonType type, Level level, AgeGroup targetAgeGroup, bool isPublished = true)
        : base(id)
    {
        Title = title;
        Description = description;
        Type = type;
        Level = level;
        TargetAgeGroup = targetAgeGroup;
        IsPublished = isPublished;
    }

    public void AddVocabularyItem(VocabularyItem item) => _vocabularyItems.Add(item);

    public void AddGrammarRule(GrammarRule rule) => _grammarRules.Add(rule);

    public void AddDailyPhrase(DailyPhrase phrase) => _dailyPhrases.Add(phrase);
}
