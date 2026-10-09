namespace WordBuddy.Content.Application.Interfaces.Autofill;

/// <summary>Non-secret auto-fill settings used by the Application layer (<c>Autofill</c> config
/// section). Bound and registered by Infrastructure.</summary>
public sealed class AutofillSettings
{
    /// <summary>Config section name.</summary>
    public const string SectionName = "Autofill";

    /// <summary>Translation locales. v1 default: Vietnamese only.</summary>
    public IReadOnlyList<string> TranslationLocales { get; init; } = ["vi"];
}
