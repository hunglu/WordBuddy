namespace WordBuddy.Content.Infrastructure.Settings;

/// <summary>Local media storage settings, bound from the <c>FileStorage</c> configuration section.</summary>
public sealed class FileStorageSettings
{
    /// <summary>Root folder new uploads are saved under (a local volume in dev/Docker; migrates to blob storage later).</summary>
    public string BasePath { get; init; } = string.Empty;
}
