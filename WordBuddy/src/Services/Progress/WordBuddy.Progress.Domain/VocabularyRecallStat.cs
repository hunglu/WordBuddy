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

    /// <summary>Points this stat at a different Content word id — used when Content merges the
    /// word this stat tracks into another word.</summary>
    public void RemapWordId(Guid newVocabularyWordId) => VocabularyWordId = newVocabularyWordId;

    /// <summary>Folds <paramref name="other"/>'s history (same user, a word Content merged into this
    /// one) into this stat: counts are summed, and the most recent check decides
    /// <see cref="Status"/>, <see cref="LastCheckedAtUtc"/> and <see cref="Word"/> (last-check-wins).</summary>
    public void MergeFrom(VocabularyRecallStat other)
    {
        TimesChecked += other.TimesChecked;
        TimesKnown += other.TimesKnown;

        if (other.LastCheckedAtUtc > LastCheckedAtUtc)
        {
            Status = other.Status;
            LastCheckedAtUtc = other.LastCheckedAtUtc;
            Word = other.Word;
        }
    }
}
