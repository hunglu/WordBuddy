using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

public sealed class DailyPhrase : Entity
{
    public Guid LessonId { get; }
    public string Phrase { get; }
    public string Translation { get; }
    public Guid? AudioAssetId { get; }
    public MediaAsset? Audio { get; }
    public Guid? VideoAssetId { get; }
    public MediaAsset? Video { get; }

    public DailyPhrase(Guid id, Guid lessonId, string phrase, string translation, Guid? audioAssetId = null, Guid? videoAssetId = null)
        : base(id)
    {
        LessonId = lessonId;
        Phrase = phrase;
        Translation = translation;
        AudioAssetId = audioAssetId;
        VideoAssetId = videoAssetId;
    }
}
