using System.Text.RegularExpressions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Domain;

/// <summary>A translation of one <see cref="Sense"/> into one language. Not exposed by any endpoint
/// yet; once exposed it follows its sense's visibility.</summary>
public sealed partial class SenseTranslation : Entity
{
    /// <summary>Maximum length of <see cref="Locale"/>.</summary>
    public const int LocaleMaxLength = 35;

    /// <summary>Maximum length of <see cref="Text"/>.</summary>
    public const int TextMaxLength = 500;

    public Guid SenseId { get; private set; }

    /// <summary>BCP-47 tag in canonical case, for example <c>vi</c>, <c>vi-VN</c>, <c>es-419</c>.</summary>
    public string Locale { get; private set; }

    public string Text { get; private set; }

    private SenseTranslation(Guid id, Guid senseId, string locale, string text)
        : base(id)
    {
        SenseId = senseId;
        Locale = locale;
        Text = text;
    }

    /// <summary>Creates a translation. Fails on a locale that is not BCP-47 shaped, or on empty or
    /// too long text. The locale's case is normalized (<c>vi-vn</c> → <c>vi-VN</c>).</summary>
    public static Result<SenseTranslation> Create(Guid id, Guid senseId, string locale, string text)
    {
        string trimmedLocale = (locale ?? string.Empty).Trim();
        if (trimmedLocale.Length > LocaleMaxLength || !LocalePattern().IsMatch(trimmedLocale))
        {
            return Result.Failure<SenseTranslation>(Error.Validation(
                "SenseTranslation.InvalidLocale",
                "The locale must be a BCP-47 tag such as 'vi' or 'vi-VN'."));
        }

        string trimmedText = (text ?? string.Empty).Trim();
        if (trimmedText.Length == 0)
        {
            return Result.Failure<SenseTranslation>(Error.Validation(
                "SenseTranslation.EmptyText",
                "A translation needs a non-empty text."));
        }

        if (trimmedText.Length > TextMaxLength)
        {
            return Result.Failure<SenseTranslation>(Error.Validation(
                "SenseTranslation.TextTooLong",
                $"A translation may have at most {TextMaxLength} characters."));
        }

        return Result.Success(new SenseTranslation(id, senseId, NormalizeLocale(trimmedLocale), trimmedText));
    }

    /// <summary>Canonical BCP-47 case: language lower, 4-letter script title, 2-letter region upper,
    /// everything else lower.</summary>
    private static string NormalizeLocale(string locale)
    {
        string[] parts = locale.Split('-');
        parts[0] = parts[0].ToLowerInvariant();
        for (int i = 1; i < parts.Length; i++)
        {
            string part = parts[i];
            parts[i] = part.Length switch
            {
                2 => part.ToUpperInvariant(),
                4 when char.IsLetter(part[0]) =>
                    char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant(),
                _ => part.ToLowerInvariant(),
            };
        }

        return string.Join('-', parts);
    }

    [GeneratedRegex("^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})*$", RegexOptions.CultureInvariant)]
    private static partial Regex LocalePattern();
}
