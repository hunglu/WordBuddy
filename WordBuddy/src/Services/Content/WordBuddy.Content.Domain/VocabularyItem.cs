using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

public sealed class VocabularyItem : Entity
{
    public Guid LessonId { get; }
    public string Word { get; }
    public string Definition { get; }
    public string Example { get; }
    public Guid? AudioAssetId { get; }
    public MediaAsset? Audio { get; }

    public VocabularyItem(Guid id, Guid lessonId, string word, string definition, string example, Guid? audioAssetId = null)
        : base(id)
    {
        LessonId = lessonId;
        Word = word;
        Definition = definition;
        Example = example;
        AudioAssetId = audioAssetId;
    }
}
