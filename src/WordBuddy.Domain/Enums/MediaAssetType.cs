namespace WordBuddy.Domain.Enums;

/// <summary>Specifies the media format of a <see cref="Entities.MediaAsset"/>.</summary>
public enum MediaAssetType
{
    /// <summary>A static image (e.g. PNG, JPEG).</summary>
    Image,

    /// <summary>An audio clip (e.g. MP3 pronunciation file).</summary>
    Audio,

    /// <summary>A video clip.</summary>
    Video,
}
