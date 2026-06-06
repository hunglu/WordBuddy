namespace WordBuddy.Domain.Entities;

/// <summary>A vocabulary word with definition, example usage, and optional media references.</summary>
public sealed class VocabularyItem
{
    /// <summary>Initializes a new <see cref="VocabularyItem"/>.</summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="lessonId">Identifier of the parent lesson.</param>
    /// <param name="word">The vocabulary word.</param>
    /// <param name="definition">Meaning of the word.</param>
    /// <param name="exampleSentence">Example sentence demonstrating word usage.</param>
    /// <param name="phoneticSpelling">IPA or simplified phonetic spelling; <see langword="null"/> if not provided.</param>
    /// <param name="imageAssetId">Identifier of the associated image <see cref="MediaAsset"/>; <see langword="null"/> if none.</param>
    /// <param name="audioAssetId">Identifier of the pronunciation audio <see cref="MediaAsset"/>; <see langword="null"/> if none.</param>
    public VocabularyItem(
        Guid id,
        Guid lessonId,
        string word,
        string definition,
        string exampleSentence,
        string? phoneticSpelling,
        Guid? imageAssetId,
        Guid? audioAssetId)
    {
        Id = id;
        LessonId = lessonId;
        Word = word;
        Definition = definition;
        ExampleSentence = exampleSentence;
        PhoneticSpelling = phoneticSpelling;
        ImageAssetId = imageAssetId;
        AudioAssetId = audioAssetId;
    }

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the identifier of the parent lesson.</summary>
    public Guid LessonId { get; }

    /// <summary>Gets the vocabulary word.</summary>
    public string Word { get; }

    /// <summary>Gets the meaning of the word.</summary>
    public string Definition { get; }

    /// <summary>Gets an example sentence demonstrating word usage.</summary>
    public string ExampleSentence { get; }

    /// <summary>Gets the IPA or simplified phonetic spelling, or <see langword="null"/> if not provided.</summary>
    public string? PhoneticSpelling { get; }

    /// <summary>Gets the <see cref="MediaAsset"/> identifier for the associated image, or <see langword="null"/> if none.</summary>
    public Guid? ImageAssetId { get; }

    /// <summary>Gets the <see cref="MediaAsset"/> identifier for the pronunciation audio, or <see langword="null"/> if none.</summary>
    public Guid? AudioAssetId { get; }
}
