namespace WordBuddy.Progress.Infrastructure.Settings;

/// <summary>The <c>ContentApi</c> configuration section — where Content lives and how often Progress
/// pulls vocabulary word-id remaps from it. Nothing here is secret.</summary>
public sealed class ContentApiSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "ContentApi";

    /// <summary>Content's base address on the internal network (not the public origin).</summary>
    public string BaseUrl { get; set; } = "http://localhost:5081";

    /// <summary>Whether the background remap sync runs at all.</summary>
    public bool RemapSyncEnabled { get; set; } = true;

    /// <summary>Time between sync runs.</summary>
    public TimeSpan RemapPollInterval { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Wait before the first run, so Content can come up.</summary>
    public TimeSpan RemapInitialDelay { get; set; } = TimeSpan.FromSeconds(15);
}
