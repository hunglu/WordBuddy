namespace WordBuddy.Infrastructure.Settings;

/// <summary>Strongly-typed settings for local file storage, bound from <c>FileStorage</c> in application configuration.</summary>
public sealed class FileStorageSettings
{
    /// <summary>Gets the root directory under which all media files are stored.</summary>
    public string BasePath { get; init; } = string.Empty;
}
