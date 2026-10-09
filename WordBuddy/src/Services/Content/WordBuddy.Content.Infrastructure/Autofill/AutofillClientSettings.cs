namespace WordBuddy.Content.Infrastructure.Autofill;

/// <summary>Settings of the external auto-fill clients (<c>Autofill</c> config section). The
/// Claude API key comes only from user-secrets or the <c>Autofill__Claude__ApiKey</c> environment
/// variable — never from a file in git.</summary>
public sealed class AutofillClientSettings
{
    /// <summary>Free Dictionary API settings.</summary>
    public DictionarySettings Dictionary { get; init; } = new();

    /// <summary>Anthropic Messages API settings.</summary>
    public ClaudeSettings Claude { get; init; } = new();

    /// <summary>Audio download settings.</summary>
    public AudioSettings Audio { get; init; } = new();

    /// <summary>Free Dictionary API.</summary>
    public sealed class DictionarySettings
    {
        /// <summary>Base URL, ending with <c>/</c>.</summary>
        public string BaseUrl { get; init; } = "https://api.dictionaryapi.dev/api/v2/entries/en/";

        /// <summary>Total timeout in seconds.</summary>
        public int TimeoutSeconds { get; init; } = 5;
    }

    /// <summary>Anthropic Messages API.</summary>
    public sealed class ClaudeSettings
    {
        /// <summary>Base URL, ending with <c>/</c>.</summary>
        public string BaseUrl { get; init; } = "https://api.anthropic.com/";

        /// <summary>Model id.</summary>
        public string Model { get; init; } = "claude-sonnet-5-5";

        /// <summary>API key. Secret: user-secrets / environment only.</summary>
        public string? ApiKey { get; init; }

        /// <summary><c>anthropic-version</c> header.</summary>
        public string ApiVersion { get; init; } = "2023-06-01";

        /// <summary>Maximum output tokens.</summary>
        public int MaxTokens { get; init; } = 4096;

        /// <summary>Total timeout in seconds.</summary>
        public int TimeoutSeconds { get; init; } = 15;
    }

    /// <summary>Audio downloads.</summary>
    public sealed class AudioSettings
    {
        /// <summary>Total timeout in seconds.</summary>
        public int TimeoutSeconds { get; init; } = 5;

        /// <summary>Largest accepted MP3, in bytes.</summary>
        public int MaxBytes { get; init; } = 2_000_000;
    }
}
