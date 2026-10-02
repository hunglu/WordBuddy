using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>A structured learning unit (vocabulary, grammar, or daily phrases). Aggregate root
/// for its <see cref="GrammarRule"/>/<see cref="DailyPhrase"/> children and its
/// <see cref="LessonVocabularyWord"/> links — only the collection matching <see cref="Type"/> is
/// expected to be populated.</summary>
public sealed class Lesson : Entity
{
    private readonly List<LessonVocabularyWord> _vocabularyWords = [];
    private readonly List<GrammarRule> _grammarRules = [];
    private readonly List<DailyPhrase> _dailyPhrases = [];

    public string Title { get; }
    public string Description { get; }
    public LessonType Type { get; }
    public Level Level { get; }
    public AgeGroup TargetAgeGroup { get; }
    public bool IsPublished { get; private set; }

    /// <summary>The lesson's words, through <see cref="LessonVocabularyWord"/>; order by
    /// <see cref="LessonVocabularyWord.SortOrder"/>.</summary>
    public IReadOnlyList<LessonVocabularyWord> VocabularyWords => _vocabularyWords;
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

    /// <summary>Appends <paramref name="word"/> to this lesson, after the existing words.</summary>
    public void AddVocabularyWord(VocabularyWord word) =>
        _vocabularyWords.Add(new LessonVocabularyWord(Id, word, _vocabularyWords.Count));

    public void AddGrammarRule(GrammarRule rule) => _grammarRules.Add(rule);

    public void AddDailyPhrase(DailyPhrase phrase) => _dailyPhrases.Add(phrase);
}
