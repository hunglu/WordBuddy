namespace WordBuddy.Domain.Entities;

/// <summary>A curated phrase surfaced to learners as a daily learning unit.</summary>
public sealed class DailyPhrase
{
    /// <summary>Initializes a new <see cref="DailyPhrase"/>.</summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="lessonId">Identifier of the parent lesson.</param>
    /// <param name="phrase">The phrase text.</param>
    /// <param name="meaning">Meaning or translation of the phrase.</param>
    /// <param name="usageContext">Context in which the phrase is typically used.</param>
    /// <param name="audioAssetId">Identifier of the pronunciation audio <see cref="MediaAsset"/>; <see langword="null"/> if none.</param>
    /// <param name="videoAssetId">Identifier of the associated video <see cref="MediaAsset"/>; <see langword="null"/> if none.</param>
    public DailyPhrase(
        Guid id,
        Guid lessonId,
        string phrase,
        string meaning,
        string usageContext,
        Guid? audioAssetId,
        Guid? videoAssetId)
    {
        Id = id;
        LessonId = lessonId;
        Phrase = phrase;
        Meaning = meaning;
        UsageContext = usageContext;
        AudioAssetId = audioAssetId;
        VideoAssetId = videoAssetId;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the identifier of the parent lesson.</summary>
    public Guid LessonId { get; }

    /// <summary>Gets the phrase text.</summary>
    public string Phrase { get; }

    /// <summary>Gets the meaning or translation of the phrase.</summary>
    public string Meaning { get; }

    /// <summary>Gets the context in which the phrase is typically used.</summary>
    public string UsageContext { get; }

    /// <summary>Gets the <see cref="MediaAsset"/> identifier for the pronunciation audio, or <see langword="null"/> if none.</summary>
    public Guid? AudioAssetId { get; }

    /// <summary>Gets the <see cref="MediaAsset"/> identifier for the associated video clip, or <see langword="null"/> if none.</summary>
    public Guid? VideoAssetId { get; }
}
