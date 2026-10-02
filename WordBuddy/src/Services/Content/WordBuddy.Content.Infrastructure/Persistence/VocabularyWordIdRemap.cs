namespace WordBuddy.Content.Infrastructure.Persistence;

/// <summary>Outbox row recording that word id <see cref="OldId"/> was merged into
/// <see cref="NewId"/> by the <c>UnifyVocabularyWords</c> migration. Other services that stored
/// <see cref="OldId"/> (Progress recall stats) must be told; <see cref="PublishedAtUtc"/> is stamped
/// once that has happened. Persistence-only — not a domain concept.</summary>
public sealed class VocabularyWordIdRemap
{
    public Guid OldId { get; private set; }
    public Guid NewId { get; private set; }
    public DateTime? PublishedAtUtc { get; private set; }

    public VocabularyWordIdRemap(Guid oldId, Guid newId)
    {
        OldId = oldId;
        NewId = newId;
    }

    /// <summary>Marks the remap as delivered.</summary>
    public void MarkPublished(DateTime publishedAtUtc) => PublishedAtUtc = publishedAtUtc;
}
