namespace WordBuddy.Progress.Infrastructure.ContentClient;

/// <summary>Where the Content service lives. Bound from <c>Services:Content</c>.</summary>
public sealed class ContentOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Services:Content";

    /// <summary>Default for local development (Content runs on 5081).</summary>
    public const string DefaultBaseUrl = "http://localhost:5081";

    /// <summary>Base URL of the Content service, for example <c>http://content-api:8080</c>.</summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;
}
