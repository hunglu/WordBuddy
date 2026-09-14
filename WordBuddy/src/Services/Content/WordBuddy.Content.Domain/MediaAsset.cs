using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>A text/image/audio/video asset. The database holds only a reference (<see cref="Url"/>) —
/// actual binary files live in blob storage or a local volume.</summary>
public sealed class MediaAsset : Entity
{
    public MediaAssetType Type { get; }
    public string Url { get; }

    public MediaAsset(Guid id, MediaAssetType type, string url) : base(id)
    {
        Type = type;
        Url = url;
    }
}
