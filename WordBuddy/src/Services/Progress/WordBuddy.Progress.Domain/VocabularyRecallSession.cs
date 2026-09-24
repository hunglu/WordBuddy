using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain;

/// <summary>One recall-check submission — a summary row purely for the progress-over-time trend
/// view (session history), separate from the per-word running totals in
/// <see cref="VocabularyRecallStat"/>.</summary>
public sealed class VocabularyRecallSession : Entity
{
    public Guid UserId { get; }
    public DateTime CheckedAtUtc { get; }
    public int WordsChecked { get; }
    public int WordsKnown { get; }

    public VocabularyRecallSession(Guid id, Guid userId, int wordsChecked, int wordsKnown) : base(id)
    {
        UserId = userId;
        CheckedAtUtc = DateTime.UtcNow;
        WordsChecked = wordsChecked;
        WordsKnown = wordsKnown;
    }
}
