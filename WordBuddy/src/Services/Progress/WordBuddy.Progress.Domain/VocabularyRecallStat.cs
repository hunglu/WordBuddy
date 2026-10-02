using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Domain;

/// <summary>Tracks one user's recall history for one personal vocabulary word.
/// <see cref="UserId"/> and <see cref="VocabularyWordId"/> are plain fields, not foreign keys —
/// <see cref="VocabularyWordId"/> is Content's own word id, and <see cref="Word"/> is a
/// denormalized copy captured at submit time (from data the frontend already fetched from
/// Content), so this service never needs a live cross-service lookup to render "words you
/// know" — consistent with the independence model.</summary>
public sealed class VocabularyRecallStat : Entity
{
    public Guid UserId { get; }
    public Guid VocabularyWordId { get; private set; }
    public string Word { get; private set; }
    public int TimesChecked { get; private set; }
    public int TimesKnown { get; private set; }
    public RecallStatus Status { get; private set; }
    public DateTime LastCheckedAtUtc { get; private set; }

    public VocabularyRecallStat(Guid id, Guid userId, Guid vocabularyWordId, string word) : base(id)
    {
        UserId = userId;
        VocabularyWordId = vocabularyWordId;
        Word = word;
        TimesChecked = 0;
        TimesKnown = 0;
        Status = RecallStatus.Learning;
        LastCheckedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Records the outcome of one recall check — last-check-wins: <see cref="Status"/>
    /// becomes <see cref="RecallStatus.Known"/> only if this, the most recent check, marked the
    /// word known.</summary>
    public void ApplyCheckResult(bool known)
    {
        TimesChecked++;
        if (known)
        {
            TimesKnown++;
        }

        Status = known ? RecallStatus.Known : RecallStatus.Learning;
        LastCheckedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Refreshes the denormalized word text — used when re-submitting a check for a word
    /// whose text has since changed in Content.</summary>
    public void UpdateWordText(string word) => Word = word;
}
