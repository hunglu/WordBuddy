namespace WordBuddy.Content.Infrastructure.Persistence;

/// <summary>Outbox row recording that word id <see cref="OldId"/> was merged into
/// <see cref="NewId"/> by the <c>UnifyVocabularyWords</c> migration. Other services that stored
/// <see cref="OldId"/> (Progress recall stats) pull pending rows from the internal
/// <c>/internal/vocabulary-remaps</c> endpoint and acknowledge them once applied.
/// Persistence-only — not a domain concept.</summary>
public sealed class VocabularyWordIdRemap
{
    public Guid OldId { get; private set; }
    public Guid NewId { get; private set; }
    /// <summary>When Progress <b>acknowledged</b> this remap (it has rewritten its stored ids).
    /// The column keeps its original name from the migration; <see langword="null"/> means pending.</summary>
    public DateTime? PublishedAtUtc { get; private set; }

    public VocabularyWordIdRemap(Guid oldId, Guid newId)
    {
        OldId = oldId;
        NewId = newId;
    }

    /// <summary>Marks the remap as acknowledged by Progress.</summary>
    public void MarkPublished(DateTime publishedAtUtc) => PublishedAtUtc = publishedAtUtc;
}
