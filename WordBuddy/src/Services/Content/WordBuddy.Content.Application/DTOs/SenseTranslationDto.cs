namespace WordBuddy.Content.Application.DTOs;

/// <summary>A sense translation: BCP-47 locale and text.</summary>
public sealed record SenseTranslationDto(string Locale, string Text);
