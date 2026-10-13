using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>A structured learning unit (vocabulary, grammar, or daily phrases). Aggregate root
/// for its <see cref="GrammarRule"/>/<see cref="DailyPhrase"/> children and its
/// <see cref="LessonSense"/> links — only the collection matching <see cref="Type"/> is
/// expected to be populated.</summary>
public sealed class Lesson : Entity
{
    private readonly List<LessonSense> _vocabularyWords = [];
    private readonly List<GrammarRule> _grammarRules = [];
    private readonly List<DailyPhrase> _dailyPhrases = [];

    public string Title { get; private set; }
    public string Description { get; private set; }
    public LessonType Type { get; }
    public Level Level { get; private set; }
    public AgeGroup TargetAgeGroup { get; private set; }
    public bool IsPublished { get; private set; }

    /// <summary>The lesson's words, through <see cref="LessonSense"/>; order by
    /// <see cref="LessonSense.SortOrder"/>.</summary>
    public IReadOnlyList<LessonSense> VocabularyWords => _vocabularyWords;
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
    public void AddVocabularyWord(Sense word) =>
        _vocabularyWords.Add(new LessonSense(Id, word, _vocabularyWords.Count));

    /// <summary>Replaces the editable details. <see cref="Type"/> never changes.</summary>
    public Result UpdateDetails(string title, string description, Level level, AgeGroup targetAgeGroup)
    {
        Title = title;
        Description = description;
        Level = level;
        TargetAgeGroup = targetAgeGroup;
        return Result.Success();
    }

    /// <summary>Makes the lesson visible to learners. Idempotent.</summary>
    public Result Publish()
    {
        IsPublished = true;
        return Result.Success();
    }

    /// <summary>Hides the lesson from learners again. Idempotent.</summary>
    public Result Unpublish()
    {
        IsPublished = false;
        return Result.Success();
    }

    /// <summary>Appends <paramref name="sense"/> at the end. Fails when the lesson is not a
    /// vocabulary lesson or the sense is already attached.</summary>
    public Result AttachSense(Sense sense)
    {
        if (Type != LessonType.Vocabulary)
        {
            return Result.Failure(Error.Validation("Lesson.NotVocabulary", $"Lesson {Id} is not a vocabulary lesson."));
        }

        if (_vocabularyWords.Any(l => l.SenseId == sense.Id))
        {
            return Result.Failure(Error.Conflict("Lesson.SenseAlreadyAttached", $"Sense {sense.Id} is already in lesson {Id}."));
        }

        _vocabularyWords.Add(new LessonSense(Id, sense, _vocabularyWords.Count));
        return Result.Success();
    }

    /// <summary>Removes the sense link and renumbers the rest to 0..n-1, keeping their order.</summary>
    public Result DetachSense(Guid senseId)
    {
        LessonSense? link = _vocabularyWords.FirstOrDefault(l => l.SenseId == senseId);
        if (link is null)
        {
            return Result.Failure(Error.NotFound("Lesson.SenseNotAttached", $"Sense {senseId} is not in lesson {Id}."));
        }

        _vocabularyWords.Remove(link);
        int position = 0;
        foreach (LessonSense remaining in _vocabularyWords.OrderBy(l => l.SortOrder).ToList())
        {
            remaining.SortOrder = position++;
        }

        return Result.Success();
    }

    /// <summary>Sets the order of the senses. <paramref name="orderedSenseIds"/> must list exactly
    /// the attached senses, each once.</summary>
    public Result ReorderSenses(IReadOnlyList<Guid> orderedSenseIds)
    {
        HashSet<Guid> requested = [.. orderedSenseIds];
        HashSet<Guid> attached = [.. _vocabularyWords.Select(l => l.SenseId)];
        if (requested.Count != orderedSenseIds.Count || !requested.SetEquals(attached))
        {
            return Result.Failure(Error.Validation(
                "Lesson.InvalidOrder", "The order must list every attached sense exactly once."));
        }

        for (int i = 0; i < orderedSenseIds.Count; i++)
        {
            _vocabularyWords.First(l => l.SenseId == orderedSenseIds[i]).SortOrder = i;
        }

        return Result.Success();
    }

    public void AddGrammarRule(GrammarRule rule) => _grammarRules.Add(rule);

    public void AddDailyPhrase(DailyPhrase phrase) => _dailyPhrases.Add(phrase);
}
